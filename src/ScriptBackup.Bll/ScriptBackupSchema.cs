using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ScriptBackup.Bll {

	public class ScriptBackupSchema : ScriptBackupBase, IScriptBackup {

		const string OutputType = "schema";

		private readonly SchemaOptions _options;

		public SchemaOptions Options {
			get { return _options; }
		}

		public ScriptBackupSchema(string serverName)
			: this(new SchemaOptions(serverName)) {
		}

		public ScriptBackupSchema(SchemaOptions options) {

			if (options == null) {
				throw new ArgumentNullException("options");
			}

			_options = options;
		}

		public void Export(string outputFile) {

			var startTime = DateTime.Now;
			var svr = _options.ServerName;

			Process((output, db, objectName, objectType) => {

				var file = new FileInfo(String.Format(outputFile, svr, db, objectName, objectType, startTime, OutputType));

				if (file.Directory != null && !file.Directory.Exists) {
					file.Directory.Create();
				}

				using (var fs = file.AppendText()) {
					fs.WriteLine(output);
				}
			});
		}

		public void Process(Action<string, string, string, string> iterator) {

			var sqlServer = Options.ServerName;
			var ops = _options.ScriptingOptions;
			var databases = _options.Databases;
			var tables = _options.Tables;

			ServerConnection conn = null;

			try {

				conn = new ServerConnection(sqlServer);

				var svr = new Server(conn);
				var walker = new DependencyWalker(svr);
				var scr = new Scripter(svr) {
					Options = ops
				};

				var dbs = ResolveDatabases(svr, databases);

				foreach (Database db in dbs) {

					ProcessDatabaseScript(db, iterator);

					var objects = new List<SqlSmoObject>();

					if (Options.ScriptTables) {
						objects.AddRange(ResolveTables(db, tables));
					}

					if (Options.ScriptViews) {
						objects.AddRange(ResolveViews(svr, db));
					}

					if (Options.ScriptProcedures) {
						objects.AddRange(ResolveProcedures(db));
					}

					if (Options.ScriptUdfs) {
						objects.AddRange(ResolveUDFs(db));
					}

					if (Options.ScriptPartitionFunctions) {
						objects.AddRange(ResolvePartitionFunctions(db));
					}

					if (Options.ScriptPartitionSchemes) {
						objects.AddRange(ResolvePartitionSchemes(db));
					}

					IEnumerable<SqlSmoObjectMeta> orderedLst = null;

					if (!Options.EnforceDependencies) {

						orderedLst = objects.Select(obj => new SqlSmoObjectMeta() {
							Name = obj.Urn.GetAttribute("Name"),
							Type = obj.Urn.Type,
							SmoObject = obj
						});

					} else {

						// Empty so prevents dependency errors
						if (!objects.Any()) {
							continue;
						}

						var tree = scr.DiscoverDependencies(objects.ToArray(), true);
						var coll = walker.WalkDependencies(tree).ToList();

						orderedLst = coll.Select(dep => new SqlSmoObjectMeta() {
							Name = dep.Urn.GetAttribute("Name"),
							Type = dep.Urn.Type,
							SmoObject = svr.GetSmoObject(dep.Urn)
						});
					}

					foreach (var mini in orderedLst) {
						ProcessScript(scr, db.Name, mini, iterator);
					}
				}
			} finally {

				if (conn != null) {
					conn.Disconnect();
				}
			}
		}

		private class SqlSmoObjectMeta {

			public string Name { get; set; }

			public string Type { get; set; }

			public SqlSmoObject SmoObject { get; set; }
		}

		private void ProcessScript(Scripter scr, string db, SqlSmoObjectMeta data, Action<string, string, string, string> iterator) {

			var name = data.SmoObject.Urn.GetAttribute("Name");
			var type = data.SmoObject.Urn.Type;
			var url = data.SmoObject;

			Console.WriteLine(type + ": " + name);

			var output = scr.Script(new[] { url });

			var sb = new StringBuilder();

			foreach (string st in output) {
				sb.AppendLine(st)
					.AppendLine("GO");
			}

			iterator(sb.ToString(), db, name, type);
		}

		private void ProcessDatabaseScript(Database db, Action<string, string, string, string> iterator) {

			Console.WriteLine("Database: " + db.Name);

			var ops = _options;
			var sb = new StringBuilder();

			if (ops.CreateDatabase != SchemaOptions.CreateDatabaseType.none) {

				if (ops.CreateDatabase == SchemaOptions.CreateDatabaseType.mini) {
					sb.Append("Create Database ").Append("[").Append(db.Name).AppendLine("]");
					sb.AppendLine("GO");
				}
				else {
				
					var dbOutput = db.Script(new ScriptingOptions() {
						NoFileGroup = true,
						TargetServerVersion = _options.ScriptingOptions.TargetServerVersion
					});

					foreach (string st in dbOutput) {

						sb.AppendLine(st);
						sb.AppendLine("GO");
					}
				}
			}

			if (ops.UseDatabase) {
				sb.Append("Use ").Append("[").Append(db.Name).AppendLine("]");
				sb.AppendLine("GO");
			}

			if (sb.Length > 0) {
				iterator(sb.ToString(), db.Name, db.Name, typeof(Database).Name);
			}
		}

		private IEnumerable<PartitionScheme> ResolvePartitionSchemes(Database db) {

			if (db.CompatibilityLevel <= CompatibilityLevel.Version80) {
				return Enumerable.Empty<PartitionScheme>();
			}

			return db.PartitionSchemes.Cast<PartitionScheme>();
		}

		private IEnumerable<PartitionFunction> ResolvePartitionFunctions(Database db) {

			if (db.CompatibilityLevel <= CompatibilityLevel.Version80) {
				return Enumerable.Empty<PartitionFunction>();
			}

			return db.PartitionFunctions.Cast<PartitionFunction>();
		}

		private IEnumerable<UserDefinedFunction> ResolveUDFs(Database db, IEnumerable<string> udfs = null) {

			var qry = db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(t => !t.IsSystemObject);

			if (udfs != null) {
				qry = qry.Where(t => udfs.Contains(t.Name));
			}

			return qry;
		}

		private IEnumerable<View> ResolveViews(Server svr, Database db, IEnumerable<string> views = null) {

			var qry = db.Views.Cast<View>().Where(t => !t.IsSystemObject);

			if (views != null) {
				qry = qry.Where(t => views.Contains(t.Name));
			}

			return qry
				;
		}

		private IEnumerable<StoredProcedure> ResolveProcedures(Database db) {

			return db.StoredProcedures.Cast<StoredProcedure>().Where(t => !t.IsSystemObject);
		}
	}
}
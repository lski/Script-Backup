using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ScriptBackup.Bll {

	public class ScriptBackupSchema : ScriptBackupBase, IScriptBackup {

		private readonly ScriptingOptions _scriptOptions;
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

			_scriptOptions = new ScriptingOptions() {
				Default = true,
				ScriptSchema = true,
				NonClusteredIndexes = true,
				WithDependencies = false,
				Triggers = true,
				TargetServerVersion = SqlServerVersion.Version110,
				ScriptDrops = false,
				Indexes = true,
				ClusteredIndexes = true,
				PrimaryObject = true,
				SchemaQualify = true,
				NoIndexPartitioningSchemes = false,
				NoFileGroup = false,
				DriPrimaryKey = true,
				DriChecks = true,
				DriAllKeys = true,
				AllowSystemObjects = false,
				IncludeIfNotExists = false,
				DriForeignKeys = true,
				DriAllConstraints = true,
				DriIncludeSystemNames = true,
				AnsiPadding = true,
				IncludeDatabaseContext = false,
				//EnforceScriptingOptions = true,
				//IncludeHeaders = true,
				//include statistics and histogram data for db clone
				//OptimizerData = true,
				//Statistics = true
			};
		}

		public void Export(string outputFile) {

			var startTime = DateTime.Now;

			Process((output, db, objectName, objectType) => {

				var file = new FileInfo(String.Format(outputFile, db, objectName, objectType, startTime));

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
			var ops = _scriptOptions;
			var databases = _options.Databases;
			var tables = _options.Tables;

			ServerConnection conn = null;

			try {

				conn = new ServerConnection(sqlServer);

				var svr = new Server(conn);
				var scr = new Scripter(svr) {
					Options = _scriptOptions
				};
				var walker = new DependencyWalker(svr);

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

					IEnumerable<Mini> orderedLst = null;

					if (true) {

						orderedLst = objects.Select(obj => new Mini() {
							Name = obj.Urn.GetAttribute("Name"),
							Type = obj.Urn.Type,
							SmoObject = obj
						});
					} else {

						var tree = scr.DiscoverDependencies(objects.ToArray(), false);
						var coll = walker.WalkDependencies(tree).ToList();

						orderedLst = coll.Select(dep => new Mini() {
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

		private class Mini {

			public string Name { get; set; }

			public string Type { get; set; }

			public SqlSmoObject SmoObject { get; set; }
		}

		private void ProcessScript(Scripter scr, string db, Mini data, Action<string, string, string, string> iterator) {

			var name = data.SmoObject.Urn.GetAttribute("Name");
			var type = data.SmoObject.Urn.Type;
			var obj = data.SmoObject;

			Console.WriteLine(type + ": " + name);

			var output = scr.Script(new[] { obj });

			var sb = new StringBuilder();

			foreach (string st in output) {
				sb.AppendLine(st)
					.AppendLine("GO");
			}

			iterator(sb.ToString(), db, name, type);
		}

		private void ProcessDatabaseScript(Database db, Action<string, string, string, string> iterator) {

			Console.WriteLine("Database: " + db.Name);

			var dbOutput = db.Script(new ScriptingOptions() {
				NoFileGroup = true,
				TargetServerVersion = _scriptOptions.TargetServerVersion
			});

			var sb = new StringBuilder();

			foreach (string st in dbOutput) {

				sb.AppendLine(st);
				sb.AppendLine("GO");
			}

			sb.Append("Use ").Append("[").Append(db.Name).AppendLine("]");
			sb.AppendLine("GO");

			iterator(sb.ToString(), db.Name, db.Name, typeof(Database).Name);
		}

		//public void Process(Action<string, string, string, string> iterator) {

		//	var sqlServer = Options.ServerName;
		//	var ops = _scriptOptions;
		//	var databases = _options.Databases;
		//	var tables = _options.Tables;

		//	ServerConnection conn = null;

		//	try {

		//		conn = new ServerConnection(sqlServer);

		//		var svr = new Server(conn);

		//		var dbs = ResolveDatabases(svr, databases);

		//		foreach (Database db in dbs) {

		//			if (Options.CreateDatabase) {
		//				ProcessDatabaseScript(db, iterator);
		//			}

		//			if (Options.ScriptTables) {
		//				ProcessTablesScript(ops, db, ResolveTables(db, tables), iterator);
		//			}

		//			if (Options.ScriptProcedures) {
		//				ProcessProceduresScript(ops, db, ResolveProcedures(db), iterator);
		//			}

		//			if (Options.ScriptUdfs) {
		//				ProcessUdfsScript(ops, db, ResolveUDFs(db), iterator);
		//			}

		//			if (Options.ScriptViews) {
		//				ProcessViewsScript(ops, db, ResolveViews(svr, db), iterator);
		//			}

		//			if (Options.ScriptPartitionFunctions) {
		//				ProcessPartitionFunctionsScript(ops, db, ResolvePartitionFunctions(db), iterator);
		//			}

		//			if (Options.ScriptPartitionSchemes) {
		//				ProcessPartitionSchemesScript(ops, db, ResolvePartitionSchemes(db), iterator);
		//			}
		//		}
		//	} finally {

		//		if (conn != null) {
		//			conn.Disconnect();
		//		}
		//	}
		//}
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
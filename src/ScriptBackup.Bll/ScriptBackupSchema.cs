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

				var dbs = ResolveDatabases(svr, databases);

				foreach (Database db in dbs) {

					if (Options.CreateDatabase) {
						ProcessDatabaseScript(db, iterator);
					}

					if (Options.ScriptTables) {
						ProcessTablesScript(ops, db, ResolveTables(db, tables), iterator);
					}

					if (Options.ScriptViews) {
						ProcessViewsScript(ops, db, ResolveViews(svr, db), iterator);
					}

					if (Options.ScriptProcedures) {
						ProcessProceduresScript(ops, db, ResolveProcedures(db), iterator);
					}

					if (Options.ScriptUdfs) {
						ProcessUdfsScript(ops, db, ResolveUDFs(db), iterator);
					}

					if (Options.ScriptPartitionFunctions) {
						ProcessPartitionFunctionsScript(ops, db, ResolvePartitionFunctions(db), iterator);
					}

					if (Options.ScriptPartitionSchemes) {
						ProcessPartitionSchemesScript(ops, db, ResolvePartitionSchemes(db), iterator);
					}
				}
			} finally {

				if (conn != null) {
					conn.Disconnect();
				}
			}
		}

		private void ProcessPartitionSchemesScript(ScriptingOptions ops, Database db, IEnumerable<PartitionScheme> partitionSchemes, Action<string, string, string, string> iterator) {

			foreach (var partitionScheme in partitionSchemes) {

				Console.WriteLine("PartitionScheme: " + partitionScheme.Name);

				var output = partitionScheme.Script(ops);

				var sb = new StringBuilder();

				foreach (string st in output) {
					sb.AppendLine(st).AppendLine("GO");
				}

				iterator(sb.ToString(), db.Name, partitionScheme.Name, typeof(PartitionScheme).Name);
			}
		}

		private IEnumerable<PartitionScheme> ResolvePartitionSchemes(Database db) {

			if (db.CompatibilityLevel <= CompatibilityLevel.Version80) {
				return Enumerable.Empty<PartitionScheme>();
			}

			return db.PartitionSchemes.Cast<PartitionScheme>();
		}

		private void ProcessPartitionFunctionsScript(ScriptingOptions ops, Database db, IEnumerable<PartitionFunction> partitionFuncs, Action<string, string, string, string> iterator) {

			foreach (var partitionFunc in partitionFuncs) {

				Console.WriteLine("PartitionFunction: " + partitionFunc.Name);

				var output = partitionFunc.Script(ops);

				var sb = new StringBuilder();

				foreach (string st in output) {
					sb.AppendLine(st).AppendLine("GO");
				}

				iterator(sb.ToString(), db.Name, partitionFunc.Name, typeof(PartitionFunction).Name);
			}
		}

		private IEnumerable<PartitionFunction> ResolvePartitionFunctions(Database db) {

			if (db.CompatibilityLevel <= CompatibilityLevel.Version80) {
				return Enumerable.Empty<PartitionFunction>();
			}

			return db.PartitionFunctions.Cast<PartitionFunction>();
		}

		private void ProcessUdfsScript(ScriptingOptions ops, Database db, IEnumerable<UserDefinedFunction> udfs, Action<string, string, string, string> iterator) {

			foreach (var udf in udfs) {

				Console.WriteLine("UDF: " + udf.Name);

				var output = udf.Script(ops);

				var sb = new StringBuilder();

				foreach (string st in output) {
					sb.AppendLine(st).AppendLine("GO");
				}

				iterator(sb.ToString(), db.Name, udf.Name, typeof(UserDefinedFunction).Name);
			}
		}

		private IEnumerable<UserDefinedFunction> ResolveUDFs(Database db, IEnumerable<string> udfs = null) {

			var qry = db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(t => !t.IsSystemObject);

			if (udfs != null) {
				qry = qry.Where(t => udfs.Contains(t.Name));
			}

			return qry;
		}

		private void ProcessViewsScript(ScriptingOptions ops, Database db, IEnumerable<View> views, Action<string, string, string, string> iterator) {

			foreach (var view in views) {

				Console.WriteLine("View: " + view.Name);

				var output = view.Script(ops);

				var sb = new StringBuilder();

				foreach (string st in output) {
					sb.AppendLine(st).AppendLine("GO");
				}

				iterator(sb.ToString(), db.Name, view.Name, typeof(View).Name);
			}
		}

		private IEnumerable<View> ResolveViews(Server svr, Database db, IEnumerable<string> views = null) {

			var qry = db.Views.Cast<View>().Where(t => !t.IsSystemObject);

			if (views != null) {
				qry = qry.Where(t => views.Contains(t.Name));
			}

			//var scr = new Scripter(svr);
			//var walker = new DependencyWalker(svr);

			//var tree = scr.DiscoverDependencies(qry.Cast<SqlSmoObject>().ToArray(), false);
			//var coll = walker.WalkDependencies(tree);

			return qry;
		}

		private void ProcessProceduresScript(ScriptingOptions ops, Database db, IEnumerable<StoredProcedure> sps, Action<string, string, string, string> iterator) {

			foreach (var sp in sps) {

				Console.WriteLine("Procedure: " + sp.Name);

				var output = sp.Script(ops);

				var sb = new StringBuilder();

				foreach (string st in output) {
					sb.AppendLine(st).AppendLine("GO");
				}

				iterator(sb.ToString(), db.Name, sp.Name, typeof(StoredProcedure).Name);
			}
		}

		private IEnumerable<StoredProcedure> ResolveProcedures(Database db) {

			return db.StoredProcedures.Cast<StoredProcedure>().Where(t => !t.IsSystemObject);
		}

		private void ProcessTablesScript(ScriptingOptions ops, Database db, IEnumerable<Table> tabs, Action<string, string, string, string> iterator) {

			foreach (var tab in tabs) {

				Console.WriteLine("Table: " + tab.Name);

				var output = tab.EnumScript(ops);

				var sb = new StringBuilder();

				foreach (string st in output) {
					sb.AppendLine(st).AppendLine("GO");
				}

				iterator(sb.ToString(), db.Name, tab.Name, typeof(Table).Name);
			}
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
	}
}
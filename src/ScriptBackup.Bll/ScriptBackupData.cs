using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.IO;

namespace ScriptBackup.Bll {

	public class ScriptBackupData : ScriptBackupBase, IScriptBackup {

		const string OutputType = "data";

		private readonly DataOptions _options;

		public DataOptions Options {
			get { return _options; }
		}

		public ScriptBackupData(string serverName)
			: this(new DataOptions(serverName)) {
		}

		public ScriptBackupData(DataOptions options) {

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

			var sqlServer = _options.ServerName;
			var ops = _options.ScriptingOptions;
			var databases = _options.Databases;
			var tables = _options.Tables;

			ServerConnection conn = null;

			try {

				conn = new ServerConnection(sqlServer);

				var svr = new Server(conn);

				var dbs = ResolveDatabases(svr, databases);

				foreach (Database db in dbs) {

					ProcessDatabaseScript(db, iterator);

					var tabs = ResolveTables(db, tables);

					foreach (Table tbl in tabs) {

						Console.WriteLine("Table: " + tbl.Name);

						var output = tbl.EnumScript(ops);

						foreach (string st in output) {
							iterator(st, db.Name, tbl.Name, typeof(Table).Name);
						}
					}
				}

			} finally {

				if (conn != null) {
					conn.Disconnect();
				}
			}
		}

		private void ProcessDatabaseScript(Database db, Action<string, string, string, string> iterator) {

			Console.WriteLine("Database: " + db.Name);

			var ops = _options;
			var sb = new StringBuilder();

			if (ops.UseDatabase) {
				sb.Append("Use ").Append("[").Append(db.Name).AppendLine("]");
				sb.AppendLine("GO");
			}

			if (sb.Length > 0) {
				iterator(sb.ToString(), db.Name, db.Name, typeof(Database).Name);
			}
		}
	}
}
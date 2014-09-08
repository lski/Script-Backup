using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.IO;

namespace ScriptBackup.Bll {

	public class ScriptBackupData : ScriptBackupBase, IScriptBackup {

		private readonly ScriptingOptions _scriptOptions;
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

			_scriptOptions = new ScriptingOptions() {
				Default = true,
				ScriptData = true,
				ScriptSchema = false,
				ScriptDrops = false,
				WithDependencies = false,
				AnsiPadding = true,
				TargetServerVersion = SqlServerVersion.Version110
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

			var sqlServer = _options.ServerName;
			var ops = _scriptOptions;
			var databases = _options.Databases;
			var tables = _options.Tables;

			ServerConnection conn = null;

			try {

				conn = new ServerConnection(sqlServer);

				var svr = new Server(conn);

				var dbs = ResolveDatabases(svr, databases);

				foreach (Database db in dbs) {

					Console.WriteLine("Database: " + db.Name);

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
	}
}
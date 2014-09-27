using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ScriptBackup.Bll {

	public class ScriptBackupData : ScriptBackupBase, IScriptBackup {

		private const string OutputType = "data";

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

			Process((output) => {

				var file = new FileInfo(String.Format(outputFile, output.Server, output.Database, output.Name, output.Type, startTime, OutputType));

				if (file.Directory != null && !file.Directory.Exists) {
					file.Directory.Create();
				}

				using (var fs = file.AppendText()) {
					fs.WriteLine(output.Output);
				}
			});
		}

		public void Process(Action<ProcessObjectOutput> iterator) {

			var connectionString = _options.ConnectionString;
			var ops = _options.ScriptingOptions;
			var databases = _options.Databases;
			var tables = _options.Tables;

			ServerConnection svrConn = null;

			var sqlConn = GetConnection(connectionString);

			Trace.Write("Running ");
			Trace.Write(OutputType);
			Trace.WriteLine(":");

			try {

				svrConn = new ServerConnection(sqlConn);

				var svr = new Server(svrConn);

				var dbs = ResolveDatabases(svr, databases);

				foreach (Database db in dbs) {

					ProcessDatabaseScript(svr.Name, db, iterator);

					var scriptData = GenerateObjectsToScript(svr, ops, Options.EnforceDependencies, ResolveTables(db, tables));

					foreach (var mini in scriptData.Objects) {
						ProcessScript(scriptData.Scripter, svr.Name, db.Name, mini, iterator);
					}
				}

			} 
			catch (Exception ex) {

				Trace.WriteLine("Script Error: " + ex.Message);

				throw new Exception("Script Error");
			}
			finally {

				if (svrConn != null) {
					svrConn.Disconnect();
				}
			}
		}

		private void ProcessScript(Scripter scr, string svr, string db, ScriptObjectMeta data, Action<ProcessObjectOutput> iterator) {

			var name = data.Name;
			var type = data.Type;
			var smoObj = data.SmoObject;

			Trace.WriteLine(type + ": " + name);

			var output = scr.EnumScript(new[] { smoObj });

			var sb = new StringBuilder();

			foreach (string st in output) {
				sb.AppendLine(st)
					.AppendLine("GO");
			}

			iterator(new ProcessObjectOutput(sb.ToString(), svr, db, name, type));
		}

		private void ProcessDatabaseScript(string svr, Database db, Action<ProcessObjectOutput> iterator) {

			Trace.WriteLine("Database: " + db.Name);

			var ops = _options;
			var sb = new StringBuilder();

			if (ops.UseDatabase) {
				sb.Append("Use ").Append("[").Append(db.Name).AppendLine("]");
				sb.AppendLine("GO");
			}

			if (sb.Length > 0) {
				iterator(new ProcessObjectOutput(sb.ToString(), svr, db.Name, db.Name, typeof(Database).Name));
			}
		}
	}
}
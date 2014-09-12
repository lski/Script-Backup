using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
				var walker = new DependencyWalker(svr);
				var scr = new Scripter(svr) {
					Options = ops
				};

				var dbs = ResolveDatabases(svr, databases);

				foreach (Database db in dbs) {

					ProcessDatabaseScript(db, iterator);

					var objects = (ResolveTables(db, tables));

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

		private void ProcessScript(Scripter scr, string db, SqlSmoObjectMeta data, Action<string, string, string, string> iterator) {

			var name = data.Name;		// .SmoObject.Urn.GetAttribute("Name");
			var type = data.Type;		// .SmoObject.Urn.Type;
			var smoObj = data.SmoObject;

			Trace.WriteLine(type + ": " + name);

			var output = scr.EnumScript(new[] { smoObj });

			var sb = new StringBuilder();

			foreach (string st in output) {
				sb.AppendLine(st)
					.AppendLine("GO");
			}

			iterator(sb.ToString(), db, name, type);
		}

		private void ProcessDatabaseScript(Database db, Action<string, string, string, string> iterator) {

			Trace.WriteLine("Database: " + db.Name);

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
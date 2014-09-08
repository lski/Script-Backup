using System.Collections.Generic;

namespace ScriptBackup.Bll {

	public class SchemaOptions : IBackupOptions {

		public SchemaOptions(string serverName) {

			ServerName = serverName;
			CreateDatabase = true;
			ScriptTables = true;
			ScriptViews = true;
			ScriptProcedures = true;
			ScriptUdfs = true;
		}

		public string ServerName { get; set; }

		public bool CreateDatabase { get; set; }

		public bool ScriptTables { get; set; }

		public bool ScriptViews { get; set; }

		public bool ScriptProcedures { get; set; }

		public bool ScriptUdfs { get; set; }

		public bool ScriptPartitionFunctions { get; set; }

		public bool ScriptPartitionSchemes { get; set; }

		public IEnumerable<string> Databases { get; set; }

		public IEnumerable<string> Tables { get; set; }
	}
}
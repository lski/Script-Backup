using System.Collections.Generic;

namespace ScriptBackup.Bll {

	public class DataOptions : IBackupOptions {

		public DataOptions(string serverName) {

			ServerName = serverName;
		}

		public DataOptions(IBackupOptions options) : this(options.ServerName) {

			Databases = options.Databases;
			Tables = options.Tables;
		}

		public string ServerName { get; set; }

		public IEnumerable<string> Databases { get; set; }

		public IEnumerable<string> Tables { get; set; }
	}
}
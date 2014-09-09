using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace ScriptBackup.Bll {

	public class DataOptions : IBackupOptions {

		private readonly ScriptingOptions _scriptOptions;

		public DataOptions(string serverName) {

			ServerName = serverName;

			UseDatabase = true;

			_scriptOptions = new ScriptingOptions() {
				AnsiPadding = true,
				Default = true,
				NoCollation = true,
				ScriptData = true,
				ScriptSchema = false,
				ScriptDrops = false,
				TargetServerVersion = SqlServerVersion.Version110,
				WithDependencies = false,
			};
		}

		public DataOptions(IBackupOptions options)
			: this(options.ServerName) {

			Databases = options.Databases;
			Tables = options.Tables;
		}

		public string ServerName { get; set; }

		public bool UseDatabase { get; set; }

		public IEnumerable<string> Databases { get; set; }

		public IEnumerable<string> Tables { get; set; }

		internal ScriptingOptions ScriptingOptions {
			get { return _scriptOptions; }
		}
	}
}
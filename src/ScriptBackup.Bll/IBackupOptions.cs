using System.Collections.Generic;

namespace ScriptBackup.Bll {

	public interface IBackupOptions {

		string ConnectionString { get; set; }

		bool UseDatabase { get; set; }

		bool EnforceDependencies { get; set; }

		IEnumerable<string> Databases { get; set; }

		IEnumerable<string> Tables { get; set; }
	}
}
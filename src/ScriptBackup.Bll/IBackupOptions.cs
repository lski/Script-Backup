using System.Collections.Generic;

namespace ScriptBackup.Bll {

	public interface IBackupOptions {

		string ServerName { get; set; }

		IEnumerable<string> Databases { get; set; }

		IEnumerable<string> Tables { get; set; }
	}
}
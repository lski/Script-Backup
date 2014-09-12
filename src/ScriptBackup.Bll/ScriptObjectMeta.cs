using Microsoft.SqlServer.Management.Smo;

namespace ScriptBackup.Bll {

	internal class ScriptObjectMeta {

		public string Name { get; set; }

		public string Type { get; set; }

		public SqlSmoObject SmoObject { get; set; }
	}
}
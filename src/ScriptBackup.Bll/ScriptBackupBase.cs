using Microsoft.SqlServer.Management.Smo;
using System.Collections.Generic;
using System.Linq;

namespace ScriptBackup.Bll {

	public abstract class ScriptBackupBase {

		protected IEnumerable<Database> ResolveDatabases(Server svr, IEnumerable<string> databases = null) {

			var qry = svr.Databases.Cast<Database>().Where(t => !t.IsSystemObject);

			if (databases != null) {
				qry = qry.Where(t => databases.Contains(t.Name));
			}

			return qry;
		}

		protected IEnumerable<Table> ResolveTables(Database db, IEnumerable<string> tables = null) {

			var qry = db.Tables.Cast<Table>().Where(t => !t.IsSystemObject);

			if (tables != null) {
				qry = qry.Where(t => tables.Contains(t.Name));
			}

			return qry;
		}
	}
}
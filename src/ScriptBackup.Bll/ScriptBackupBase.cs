using System;
using System.Data.SqlClient;
using System.Diagnostics;
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

		internal ScriptObjectsMeta GenerateObjectsToScript(Server svr, ScriptingOptions ops, bool enforceDependancies, IEnumerable<SqlSmoObject> objects) {

			// Return an empty object if there are no objects, this prevents extra processing and prevents errors on walking dependencies
			if (!objects.Any()) {
				return new ScriptObjectsMeta(null, Enumerable.Empty<ScriptObjectMeta>());
			}

			var walker = new DependencyWalker(svr);
			var scr = new Scripter(svr) {
				Options = ops
			};

			IEnumerable<ScriptObjectMeta> orderedLst = null;

			if (!enforceDependancies) {

				orderedLst = objects.Select(obj => new ScriptObjectMeta() {
					Name = obj.Urn.GetAttribute("Name"),
					Type = obj.Urn.Type,
					SmoObject = obj
				});

			} else {

				var tree = scr.DiscoverDependencies(objects.ToArray(), true);
				var coll = walker.WalkDependencies(tree).ToList();

				orderedLst = coll.Select(dep => new ScriptObjectMeta() {
					Name = dep.Urn.GetAttribute("Name"),
					Type = dep.Urn.Type,
					SmoObject = svr.GetSmoObject(dep.Urn)
				});
			}

			return new ScriptObjectsMeta(scr, orderedLst);
		}

		/// <summary>
		/// If a connection is not returned the test connection failed
		/// </summary>
		/// <returns></returns>
		internal SqlConnection GetConnection(string connectionString) {

			SqlConnection conn;

			try {

				conn = new SqlConnection(connectionString);

				conn.Open();
				conn.Close();
			}
			catch (Exception ex) {
				
				Trace.WriteLine("Connection failed:");
				Trace.WriteLine(ex.Message);

				throw new Exception("Connection failed");
			}

			return conn;
		}
	}
}
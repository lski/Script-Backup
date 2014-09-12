using Microsoft.SqlServer.Management.Smo;
using System.Collections.Generic;

namespace ScriptBackup.Bll {

	internal class ScriptObjectsMeta {

		public ScriptObjectsMeta(Scripter scripter, IEnumerable<ScriptObjectMeta> objects) {

			Objects = objects;
			Scripter = scripter;
		}

		public Scripter Scripter { get; set; }

		public IEnumerable<ScriptObjectMeta> Objects { get; private set; }
	}
}
using System;

namespace ScriptBackup.Bll {

	public interface IScriptBackup {

		void Export(string outputFile);

		void Process(Action<ProcessObjectOutput> iterator);
	}
}
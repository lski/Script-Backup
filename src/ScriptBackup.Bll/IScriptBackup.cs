using System;

namespace ScriptBackup.Bll {

	public interface IScriptBackup {

		void Export(string outputFile);

		void Process(Action<string, string, string, string> iterator);
	}
}
using System;

namespace ScriptBackup.Bll {

	public class ScriptBackup : IScriptBackup {

		private readonly ScriptBackupSchema _scriptBackupSchema;
		private readonly ScriptBackupData _scriptBackupData;

		public ScriptBackup(string serverName)
			: this(new SchemaOptions(serverName)) {
		}

		public ScriptBackup(SchemaOptions options) {

			_scriptBackupSchema = new ScriptBackupSchema(options);
			_scriptBackupData = new ScriptBackupData(new DataOptions(options));
		}

		public void Export(string outputFile) {

			// TODO: Do some refactoring

			Console.WriteLine("Schema");
			_scriptBackupSchema.Export(outputFile);
			Console.WriteLine("Data");
			_scriptBackupData.Export(outputFile);
		}

		public void Process(Action<string, string, string, string> iterator) {

			_scriptBackupSchema.Process(iterator);
			_scriptBackupData.Process(iterator);
		}
	}
}
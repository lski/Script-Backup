using System;
using System.Text.RegularExpressions;

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

			outputFile = ResolveOutputFile(outputFile);

			_scriptBackupSchema.Export(outputFile);
			_scriptBackupData.Export(outputFile);
		}

		public void Process(Action<string, string, string, string> iterator) {

			_scriptBackupSchema.Process(iterator);
			_scriptBackupData.Process(iterator);
		}

		private string ResolveOutputFile(string outputFile) {

			var reg = "{3([^}]*)?}";
			var matches = Regex.Match(outputFile, reg);

			// If there was a match attempt to replace the date format position
			if (matches.Success) {

				var format = String.IsNullOrEmpty(matches.Groups[1].Value) ? "{0}" : "{0" + matches.Groups[1].Value + "}";
				var newValue = String.Format(format, DateTime.Now);

				outputFile = Regex.Replace(outputFile, reg, newValue);
			}

			return outputFile;
		}
	}
}
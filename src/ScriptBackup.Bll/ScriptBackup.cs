using System;
using System.Text.RegularExpressions;

namespace ScriptBackup.Bll {

	public class ScriptBackup : IScriptBackup {

		internal const int FormatPositionStartTime = 4;

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

			// If the output is not split on the output type, then remove the need for the UseDatabase from the data section as it should be in the same file
			_scriptBackupData.Options.UseDatabase = !(outputFile.Contains("{5"));
			_scriptBackupData.Export(outputFile);
		}

		public void Process(Action<string, string, string, string> iterator) {

			_scriptBackupSchema.Process(iterator);
			_scriptBackupData.Process(iterator);
		}

		private string ResolveOutputFile(string outputFile) {

			var reg = "{" + FormatPositionStartTime + "([^}]*)?}";
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
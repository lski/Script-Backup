using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptBackup.Bll {

	internal class Program {

		private static void Main(string[] args) {

			string type = null;
			string server = null;
			string output = null;
			string[] dbs = null;

			type = (from arg in args where arg.StartsWith("-type:") select arg.Replace("-type:", "")).FirstOrDefault() ?? "all";
			server = (from arg in args where arg.StartsWith("-server:") select arg.Replace("-server:", "")).FirstOrDefault();
			output = (from arg in args where arg.StartsWith("-output:") select arg.Replace("-output:", "")).FirstOrDefault();
			dbs = (from arg in args where arg.StartsWith("-dbs:") select arg.Replace("-dbs:", "").Split(',')).FirstOrDefault();

			if (String.IsNullOrEmpty(server)) {
				WriteFullMessage("The server argument is required");
				return;
			}

			if (String.IsNullOrEmpty(output)) {
				WriteFullMessage("The output argument is required");
				return;
			}

			switch (type) {
				case "all":
					All(server, output, dbs);
					break;

				case "schema":
					Schema(server, output, dbs);
					break;

				case "data":
					Data(server, output, dbs);
					break;
			}

			WriteFullMessage("");
		}

		private static void All(string server, string output, IEnumerable<string> dbs) {

			var backup = new ScriptBackup(new SchemaOptions(server) {
				ScriptPartitionFunctions = true,
				ScriptPartitionSchemes = true,
				Databases = dbs
			});

			backup.Export(output);
		}

		private static void Schema(string server, string output, IEnumerable<string> dbs) {

			var backup = new ScriptBackupSchema(new SchemaOptions(server) {
				ScriptPartitionFunctions = true,
				ScriptPartitionSchemes = true,
				Databases = dbs
			});

			backup.Export(output);
		}

		private static void Data(string server, string output, IEnumerable<string> dbs) {

			var backup = new ScriptBackupData(new DataOptions(new DataOptions(server) {
				Databases = dbs
			}));

			backup.Export(output);
		}

		private static void WriteFullMessage(params string[] messages) {

			foreach (var mess in messages) {
				Console.WriteLine(mess);
			}

			Console.WriteLine("Press any key to continue");
			Console.ReadLine();
		}
	}
}
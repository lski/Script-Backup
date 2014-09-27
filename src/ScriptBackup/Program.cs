using System.Diagnostics;
using System.Text.RegularExpressions;
using ScriptBackup.Bll;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptBackup {

	internal class Program {

		private static void Main(string[] args) {

			string type = null;
			string connectionString = null;
			string output = null;
			string[] dbs = null;
			bool silent = false;

			type = (from arg in args where arg.StartsWith("-type:") select arg.Replace("-type:", "")).FirstOrDefault() ?? "all";
			output = (from arg in args where arg.StartsWith("-output:") select arg.Replace("-output:", "")).FirstOrDefault();
			connectionString = (from arg in args where arg.StartsWith("-connection:") select arg.Replace("-connection:", "")).FirstOrDefault();
			dbs = (from arg in args where arg.StartsWith("-dbs:") select Regex.Replace(arg, "(-dbs:| )", "").Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)).FirstOrDefault();
			silent = args.Contains("-silent");

			if (!silent) {
				Trace.Listeners.Add(new ConsoleTraceListener());
			}

			try {

				if (String.IsNullOrEmpty(connectionString)) {

					connectionString = CreateConnectionString((from arg in args where arg.StartsWith("-server:") select arg.Replace("-server:", "")).FirstOrDefault());

					// Check again if not set from the server name either
					if (String.IsNullOrEmpty(connectionString)) {
						WriteMultipleLines("The connection string or server argument is required");
						return;
					}
				}

				if (String.IsNullOrEmpty(output)) {
					WriteMultipleLines("The output argument is required");
					return;
				}

				// Only show if the rest of the output is hidden
				if (silent) {
					Console.WriteLine("Running...");
				}
				
				switch (type) {
					case "all":
						All(connectionString, output, dbs);
						break;

					case "schema":
						Schema(connectionString, output, dbs);
						break;

					case "data":
						Data(connectionString, output, dbs);
						break;
				}
			}
			catch (Exception ex) {

				WriteMultipleLines("Sorry an error occurred", ex.Message);
				Console.ReadLine();
			}
			finally {

				if (!silent) {
					WriteMultipleLines("", "Press any key to continue");
					Console.ReadLine();
				}
			}
		}

		private static void All(string server, string output, IEnumerable<string> dbs) {

			var backup = new ScriptBackupAll(new SchemaOptions(server) {
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

		private static void WriteMultipleLines(params string[] messages) {

			foreach (var mess in messages) {
				Console.WriteLine(mess);
			}
		}

		private static string CreateConnectionString(string server) {

			if (String.IsNullOrEmpty(server)) {
				return null;
			}

			return "Server=" + server + ";Trusted_Connection=True;";
		}
	}
}
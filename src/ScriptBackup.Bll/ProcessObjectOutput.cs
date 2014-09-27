namespace ScriptBackup.Bll {

	public class ProcessObjectOutput {

		public ProcessObjectOutput(string output, string server, string database, string name, string type) {
			Output = output;
			Server = server;
			Database = database;
			Name = name;
			Type = type;
		}

		public string Output { get; set; }

		public string Server { get; set; }

		public string Database { get; set; }

		public string Name { get; set; }

		public string Type { get; set; }
	}
}
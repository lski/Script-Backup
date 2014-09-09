using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

namespace ScriptBackup.Bll {

	public class SchemaOptions : IBackupOptions {

		public enum CreateDatabaseType {
			none = 0,
			mini = 1,
			full = 2
		}

		private readonly ScriptingOptions _scriptOptions;

		public SchemaOptions(string serverName) {

			ServerName = serverName;
			CreateDatabase = CreateDatabaseType.mini;
			UseDatabase = true;
			ScriptTables = true;
			ScriptViews = true;
			ScriptProcedures = true;
			ScriptUdfs = true;
			EnforceDependencies = true;

			_scriptOptions = new ScriptingOptions() {
				AllowSystemObjects = false,
				AnsiPadding = true,
				ClusteredIndexes = true,
				Default = true,
				DriAllConstraints = true,
				DriAllKeys = true,
				DriChecks = true,
				DriForeignKeys = true,
				DriIncludeSystemNames = true,
				DriPrimaryKey = true,
				IncludeDatabaseContext = false,
				IncludeIfNotExists = false,
				Indexes = true,
				NoCollation = true,
				NoFileGroup = false,
				NoIndexPartitioningSchemes = false,
				NonClusteredIndexes = true,
				PrimaryObject = true,
				SchemaQualify = true,
				ScriptDrops = false,
				ScriptSchema = true,
				TargetServerVersion = SqlServerVersion.Version110,
				Triggers = true,
				WithDependencies = false,
			};
		}

		public bool UseDatabase { get; set; }

		public string ServerName { get; set; }

		public CreateDatabaseType CreateDatabase { get; set; }

		public bool ScriptTables { get; set; }

		public bool ScriptViews { get; set; }

		public bool ScriptProcedures { get; set; }

		public bool ScriptUdfs { get; set; }

		public bool ScriptPartitionFunctions { get; set; }

		public bool ScriptPartitionSchemes { get; set; }

		public IEnumerable<string> Databases { get; set; }

		public IEnumerable<string> Tables { get; set; }

		public bool EnforceDependencies { get; set; }

		internal ScriptingOptions ScriptingOptions {
			get { return _scriptOptions; }
		}
	}
}
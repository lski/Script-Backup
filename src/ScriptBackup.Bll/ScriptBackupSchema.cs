﻿using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ScriptBackup.Bll {

	public class ScriptBackupSchema : ScriptBackupBase, IScriptBackup {

		const string OutputType = "schema";

		private readonly SchemaOptions _options;

		public SchemaOptions Options {
			get { return _options; }
		}

		public ScriptBackupSchema(string serverName)
			: this(new SchemaOptions(serverName)) {
		}

		public ScriptBackupSchema(SchemaOptions options) {

			if (options == null) {
				throw new ArgumentNullException("options");
			}

			_options = options;
		}

		public void Export(string outputFile) {

			var startTime = DateTime.Now;

			Process((output) => {

				var file = new FileInfo(String.Format(outputFile, output.Server, output.Database, output.Name, output.Type, startTime, OutputType));

				if (file.Directory != null && !file.Directory.Exists) {
					file.Directory.Create();
				}

				using (var fs = file.AppendText()) {
					fs.WriteLine(output.Output);
				}
			});
		}

		public void Process(Action<ProcessObjectOutput> iterator) {

			var connectionString = Options.ConnectionString;
			var ops = _options.ScriptingOptions;
			var databases = _options.Databases;
			var tables = _options.Tables;

			ServerConnection svrConn = null;

			var sqlConn = GetConnection(connectionString);

			Trace.Write("Running ");
			Trace.Write(OutputType);
			Trace.WriteLine(":");

			try {

				svrConn = new ServerConnection(sqlConn);

				var svr = new Server(svrConn);

				var dbs = ResolveDatabases(svr, databases);

				foreach (Database db in dbs) {

					ProcessDatabaseScript(svr.Name, db, iterator);

					var objects = new List<SqlSmoObject>();

					if (Options.ScriptTables) {
						objects.AddRange(ResolveTables(db, tables));
					}

					if (Options.ScriptViews) {
						objects.AddRange(ResolveViews(svr, db));
					}

					if (Options.ScriptProcedures) {
						objects.AddRange(ResolveProcedures(db));
					}

					if (Options.ScriptUdfs) {
						objects.AddRange(ResolveUDFs(db));
					}

					if (Options.ScriptPartitionFunctions) {
						objects.AddRange(ResolvePartitionFunctions(db));
					}

					if (Options.ScriptPartitionSchemes) {
						objects.AddRange(ResolvePartitionSchemes(db));
					}

					var scriptData = GenerateObjectsToScript(svr, ops, Options.EnforceDependencies, objects);

					foreach (var mini in scriptData.Objects) {
						ProcessScript(scriptData.Scripter, svr.Name, db.Name, mini, iterator);
					}
				}
			}
			catch(ConnectionFailureException cex){

				Trace.WriteLine("Connection Error:");
				Trace.WriteLine("\t" + cex.Message);

				throw cex;
			}
			catch (Exception ex) {
				
				Trace.WriteLine("Script error:");
				Trace.WriteLine(ex.Message);

				throw new Exception("Script error");
			}
			finally {

				if (svrConn != null) {
					svrConn.Disconnect();
				}
			}
		}

		private void ProcessScript(Scripter scr, string svr, string db, ScriptObjectMeta data, Action<ProcessObjectOutput> iterator) {

			var name = data.Name;		// .SmoObject.Urn.GetAttribute("Name");
			var type = data.Type;		// .SmoObject.Urn.Type;
			var smoObj = data.SmoObject;

			Trace.WriteLine(type + ": " + name);

			var output = scr.Script(new[] { smoObj });

			var sb = new StringBuilder();

			foreach (string st in output) {
				sb.AppendLine(st)
					.AppendLine("GO");
			}

			iterator(new ProcessObjectOutput(sb.ToString(), svr, db, name, type));
		}

		private void ProcessDatabaseScript(string svr, Database db, Action<ProcessObjectOutput> iterator) {

			Trace.WriteLine("Database: " + db.Name);

			var ops = _options;
			var sb = new StringBuilder();

			if (ops.CreateDatabase != SchemaOptions.CreateDatabaseType.none) {

				if (ops.CreateDatabase == SchemaOptions.CreateDatabaseType.mini) {
					sb.Append("Create Database ").Append("[").Append(db.Name).AppendLine("]");
					sb.AppendLine("GO");
				}
				else {
				
					var dbOutput = db.Script(new ScriptingOptions() {
						NoFileGroup = true,
						TargetServerVersion = _options.ScriptingOptions.TargetServerVersion
					});

					foreach (string st in dbOutput) {

						sb.AppendLine(st);
						sb.AppendLine("GO");
					}
				}
			}

			if (ops.UseDatabase) {
				sb.Append("Use ").Append("[").Append(db.Name).AppendLine("]");
				sb.AppendLine("GO");
			}

			if (sb.Length > 0) {
				iterator(new ProcessObjectOutput(sb.ToString(), svr, db.Name, db.Name, typeof(Database).Name));
			}
		}

		private IEnumerable<PartitionScheme> ResolvePartitionSchemes(Database db) {

			if (db.CompatibilityLevel <= CompatibilityLevel.Version80) {
				return Enumerable.Empty<PartitionScheme>();
			}

			return db.PartitionSchemes.Cast<PartitionScheme>();
		}

		private IEnumerable<PartitionFunction> ResolvePartitionFunctions(Database db) {

			if (db.CompatibilityLevel <= CompatibilityLevel.Version80) {
				return Enumerable.Empty<PartitionFunction>();
			}

			return db.PartitionFunctions.Cast<PartitionFunction>();
		}

		private IEnumerable<UserDefinedFunction> ResolveUDFs(Database db, IEnumerable<string> udfs = null) {

			var qry = db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(t => !t.IsSystemObject);

			if (udfs != null) {
				qry = qry.Where(t => udfs.Contains(t.Name));
			}

			return qry;
		}

		private IEnumerable<View> ResolveViews(Server svr, Database db, IEnumerable<string> views = null) {

			var qry = db.Views.Cast<View>().Where(t => !t.IsSystemObject);

			if (views != null) {
				qry = qry.Where(t => views.Contains(t.Name));
			}

			return qry
				;
		}

		private IEnumerable<StoredProcedure> ResolveProcedures(Database db) {

			return db.StoredProcedures.Cast<StoredProcedure>().Where(t => !t.IsSystemObject);
		}
	}
}
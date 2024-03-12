namespace RobinHood70.HoodBot.Design
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using MySql.Data.MySqlClient;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;

	// This is a quick conversion from a static class to a standard class. This can probably be converted to use (or inherit from) ADO.NET classes at some point, but for now, I'm leaving this as close to the original code as possible for an easy changeover.
	public class Database
	{
		#region Constructors
		public Database(string connectionString)
		{
			this.ConnectionString = connectionString.NotNull();
		}
		#endregion

		#region Public Properties
		public string ConnectionString { get; }
		#endregion

		#region Public Static Methods
		public static IEnumerable<IDataRecord> RunQuery(string connectionString, string query) => RunQuery(connectionString, query, -1);

		public static IEnumerable<IDataRecord> RunQuery(string connectionString, string query, long pageSize)
		{
			using MySqlConnection connection = new(connectionString);
			connection.Open();
			if (pageSize <= 0)
			{
				pageSize = long.MaxValue;
			}

			for (long offset = 0; true; offset += pageSize)
			{
				var limitedQuery = query + (pageSize == long.MaxValue ? string.Empty : $" LIMIT {offset}, {pageSize}");
				using MySqlCommand command = new(limitedQuery, connection);
				using var reader = command.ExecuteReader();
				var recordsRead = 0;
				var readResult = reader.Read();
				while (readResult)
				{
					yield return reader;
					readResult = reader.Read();
					recordsRead++;
				}

				if (recordsRead < pageSize)
				{
					yield break;
				}
			}
		}

		public static IEnumerable<T> RunQuery<T>(string connectionString, string query, Func<IDataRecord, T> factory) => RunQuery(connectionString, query, -1, factory);

		public static IEnumerable<T> RunQuery<T>(string connectionString, string query, long pageSize, Func<IDataRecord, T> factory)
		{
			foreach (var row in RunQuery(connectionString, query, pageSize))
			{
				yield return factory(row);
			}
		}
		#endregion

		#region Public Methods
		public IEnumerable<IDataRecord> RunQuery(string query) => RunQuery(this.ConnectionString, query, -1);

		public IEnumerable<IDataRecord> RunQuery(string query, long pageSize) => RunQuery(this.ConnectionString, query, pageSize);

		public IEnumerable<T> RunQuery<T>(string query, Func<IDataRecord, T> factory) => RunQuery(this.ConnectionString, query, -1, factory);

		public IEnumerable<T> RunQuery<T>(string query, long pageSize, Func<IDataRecord, T> factory) => RunQuery(this.ConnectionString, query, pageSize, factory);

		public IEnumerable<string> ShowTables() => this.ShowTables(string.Empty);

		public IEnumerable<string> ShowTables(string? prefix)
		{
			var query = "SHOW TABLES";
			if (!string.IsNullOrEmpty(prefix))
			{
				query += $" LIKE {prefix}%";
			}

			foreach (var row in this.RunQuery(query))
			{
				yield return EsoLog.ConvertEncoding((string)row[0]);
			}
		}
		#endregion
	}
}
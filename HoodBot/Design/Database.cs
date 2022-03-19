namespace RobinHood70.HoodBot.Design
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using MySql.Data.MySqlClient;

	public static class Database
	{
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
				Debug.WriteLine(limitedQuery.Replace('\n', ' '));
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
	}
}
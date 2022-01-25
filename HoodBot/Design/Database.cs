namespace RobinHood70.HoodBot.Design
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using MySql.Data.MySqlClient;

	public static class Database
	{
		public static IEnumerable<IDataRecord> RunQuery(string connectionString, string query)
		{
			using MySqlConnection connection = new(connectionString);
			connection.Open();
			using MySqlCommand command = new(query, connection);
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				yield return reader;
			}
		}

		public static IEnumerable<T> RunQuery<T>(string connectionString, string query, Func<IDataRecord, T> factory)
		{
			// Could also just call non-generic RunQuery but it seemed better to not have two levels of yield return when it's so easy to have only one.
			using MySqlConnection connection = new(connectionString);
			connection.Open();
			using MySqlCommand command = new(query, connection);
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				yield return factory(reader);
			}
		}
	}
}
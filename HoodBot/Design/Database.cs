namespace RobinHood70.HoodBot.Design
{
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
	}
}

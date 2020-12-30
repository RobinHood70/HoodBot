namespace RobinHood70.HoodBot.Design
{
	using System.Collections.Generic;
	using System.Data;
	using MySql.Data.MySqlClient;

	public static class Database
	{
		public static IEnumerable<IDataRecord> RunQuery(string connectionString, string query)
		{
			using var connection = new MySqlConnection(connectionString);
			connection.Open();
			using var command = new MySqlCommand(query, connection);
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				yield return reader;
			}
		}
	}
}

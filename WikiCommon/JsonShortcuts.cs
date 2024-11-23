namespace RobinHood70.WikiCommon
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	/// <summary>Provides useful shortcut JSON-related methods.</summary>
	public static class JsonShortcuts
	{
		/// <summary>Loads a JSON file into a JToken.</summary>
		/// <param name="fileName">The file name.</param>
		/// <returns>The parsed JToken object.</returns>
		public static JToken Load(string fileName)
		{
			ArgumentNullException.ThrowIfNull(fileName);
			using var stream = new StreamReader(fileName);
			using var jsonStream = new JsonTextReader(stream);
			return JToken.Load(jsonStream);
		}

		/// <summary>Attempts to get the named JToken object and throws if it's not present.</summary>
		/// <param name="token">The token to use.</param>
		/// <param name="name">The name to search for.</param>
		/// <returns>The named JToken object.</returns>
		/// <exception cref="InvalidDataException">Thrown if the value is not found.</exception>
		public static JToken MustHave([NotNull] this JToken token, string name)
		{
			ArgumentNullException.ThrowIfNull(token);
			ArgumentNullException.ThrowIfNull(name);
			return token[name] ?? throw new InvalidDataException();
		}

		/// <summary>Attempts to get the named JToken object as a double and throws if it's not present.</summary>
		/// <param name="token">The token to use.</param>
		/// <param name="name">The name to search for.</param>
		/// <returns>The named JToken object as a double.</returns>
		/// <exception cref="InvalidDataException">Thrown if the value is not found.</exception>
		public static double MustHaveDouble([NotNull] this JToken token, string name)
		{
			ArgumentNullException.ThrowIfNull(token);
			ArgumentNullException.ThrowIfNull(name);
			var obj = token[name] ?? throw new InvalidDataException();
			return (double)obj;
		}

		/// <summary>Attempts to get the named JToken object as an integer and throws if it's not present.</summary>
		/// <param name="token">The token to use.</param>
		/// <param name="name">The name to search for.</param>
		/// <returns>The named JToken object as an integer.</returns>
		/// <exception cref="InvalidDataException">Thrown if the value is not found.</exception>
		public static int MustHaveInt([NotNull] this JToken token, string name)
		{
			ArgumentNullException.ThrowIfNull(token);
			ArgumentNullException.ThrowIfNull(name);
			var obj = token[name] ?? throw new InvalidDataException();
			return (int)obj;
		}

		/// <summary>Attempts to get the named JToken object as a long and throws if it's not present.</summary>
		/// <param name="token">The token to use.</param>
		/// <param name="name">The name to search for.</param>
		/// <returns>The named JToken object as a long.</returns>
		/// <exception cref="InvalidDataException">Thrown if the value is not found.</exception>
		public static long MustHaveLong([NotNull] this JToken token, string name)
		{
			ArgumentNullException.ThrowIfNull(token);
			ArgumentNullException.ThrowIfNull(name);
			var obj = token[name] ?? throw new InvalidDataException();
			return (long)obj;
		}

		/// <summary>Attempts to get the named JToken object as a string and throws if it's not present or the value is null.</summary>
		/// <param name="token">The token to use.</param>
		/// <param name="name">The name to search for.</param>
		/// <returns>The named JToken object.</returns>
		/// <exception cref="InvalidDataException">Thrown if the value is not found.</exception>
		public static string MustHaveString([NotNull] this JToken token, string name)
		{
			ArgumentNullException.ThrowIfNull(token);
			ArgumentNullException.ThrowIfNull(name);
			var obj = token[name] ?? throw new InvalidDataException();
			return (string?)obj ?? throw new InvalidDataException();
		}
	}
}
namespace RobinHood70.HoodBot.Design
{
	using System.IO;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	// While it would make sense to make the entire class generic, not doing so allows type inference for Save.
	internal static class Settings
	{
		#region Public Properties
		public static TextEncrypter Encrypter { get; } = new TextEncrypter("¡ʇᴉ ǝʇɐɔsnɟqO");
		#endregion

		#region Public Methods
		public static T Load<T>()
			where T : IJsonSettings<T>, new() => Load<T>(false);

		public static T Load<T>(bool mustExist)
			where T : IJsonSettings<T>, new()
		{
			var settingsFile = new T();
			try
			{
				using var file = File.OpenText(settingsFile.FileName);
				using var reader = new JsonTextReader(file);
				var json = JObject.Load(reader);
				while (reader.Read())
				{
				}

				settingsFile.FromJson(json);
			}
			catch (FileNotFoundException)
			{
				if (mustExist)
				{
					throw;
				}
			}

			return settingsFile;
		}

		public static void Save<T>(T settings)
			where T : IJsonSettings<T>, new()
		{
			var json = settings.ToJson();
			using var textWriter = File.CreateText(settings.FileName);
			using var writer = new JsonTextWriter(textWriter)
			{
				Formatting = Formatting.Indented,
				IndentChar = '\t',
				Indentation = 1
			};
			json.WriteTo(writer);
		}
	}
	#endregion
}
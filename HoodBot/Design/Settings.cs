namespace RobinHood70.HoodBot.Design;

using System.Diagnostics;
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
		T settingsFile = new();
		if (File.Exists(settingsFile.FileName))
		{
			using var file = File.OpenText(settingsFile.FileName);
			using JsonTextReader reader = new(file);
			var json = JObject.Load(reader);
			while (reader.Read())
			{
				// Only needs to read until done (json is filled).
			}

			settingsFile.FromJson(json);
		}
		else if (mustExist)
		{
			var msg = $"{settingsFile.FileName} not found!";
			Debug.WriteLine(msg); // In case this occurs in an initializer, this makes it easier to debug.
			throw new FileNotFoundException(msg, settingsFile.FileName);
		}

		return settingsFile;
	}

	public static void Save<T>(T settings)
		where T : IJsonSettings<T>, new()
	{
		var json = settings.ToJson();
		var path = Path.GetDirectoryName(settings.FileName);
		if (!string.IsNullOrEmpty(path))
		{
			Directory.CreateDirectory(path);
		}

		using var textWriter = File.CreateText(settings.FileName);
		using JsonTextWriter writer = new(textWriter)
		{
			Formatting = Formatting.Indented,
			IndentChar = '\t',
			Indentation = 1
		};
		json.WriteTo(writer);
	}
	#endregion
}
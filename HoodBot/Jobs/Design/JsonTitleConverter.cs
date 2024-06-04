namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class JsonTitleConverter(Site site) : JsonConverter<Title>
	{
		#region Public Override Methods
		public override Title ReadJson(JsonReader reader, Type objectType, Title existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			ArgumentNullException.ThrowIfNull(reader);
			var title = (string)reader
				.Value
				.PropertyNotNull(nameof(reader), nameof(reader.Value));
			return TitleFactory.FromUnvalidated(site, title);
		}

		public override void WriteJson(JsonWriter writer, Title value, JsonSerializer serializer)
		{
			ArgumentNullException.ThrowIfNull(writer);
			writer.WriteValue(value.FullPageName());
		}
		#endregion
	}
}
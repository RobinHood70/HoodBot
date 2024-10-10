namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	[Obsolete("Appears to be unused")]
	public class JsonTitleConverter(Site site) : JsonConverter<Title>
	{
		#region Public Override Methods
		public override Title ReadJson(JsonReader reader, Type objectType, Title? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			ArgumentNullException.ThrowIfNull(reader);
			return reader.Value is string title
				? TitleFactory.FromUnvalidated(site, title).ToTitle()
				: throw new InvalidOperationException("Value property should not be null.");
		}

		public override void WriteJson(JsonWriter writer, Title? value, JsonSerializer serializer)
		{
			ArgumentNullException.ThrowIfNull(writer);
			writer.WriteValue(value?.FullPageName());
		}
		#endregion
	}
}
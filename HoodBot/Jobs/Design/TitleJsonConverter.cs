namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using RobinHood70.Robby;
	using static RobinHood70.WikiCommon.Globals;

	public class TitleJsonConverter : JsonConverter<Title>
	{
		private readonly Site site;

		public TitleJsonConverter(Site site) => this.site = site;

		public override Title ReadJson(JsonReader reader, Type objectType, Title existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			ThrowNull(reader, nameof(reader));
			ThrowNull(reader.Value, nameof(reader), nameof(reader.Value));
			return new Title(this.site, (string)reader.Value);
		}

		public override void WriteJson(JsonWriter writer, Title value, JsonSerializer serializer)
		{
			ThrowNull(writer, nameof(writer));
			ThrowNull(value, nameof(value));
			JToken.FromObject(value.FullPageName).WriteTo(writer);
		}
	}
}

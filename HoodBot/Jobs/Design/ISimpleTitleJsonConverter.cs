namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.WikiCommon.Globals;

	public class ISimpleTitleJsonConverter : JsonConverter<ISimpleTitle>
	{
		private readonly Site site;

		public ISimpleTitleJsonConverter(Site site) => this.site = site;

		public override ISimpleTitle ReadJson(JsonReader reader, Type objectType, ISimpleTitle existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			ThrowNull(reader, nameof(reader));
			ThrowNull(reader.Value, nameof(reader), nameof(reader.Value));
			return new FullTitle(this.site, (string)reader.Value);
		}

		public override void WriteJson(JsonWriter writer, ISimpleTitle value, JsonSerializer serializer)
		{
			ThrowNull(writer, nameof(writer));
			ThrowNull(value, nameof(value));
			JToken.FromObject(value.ToString() ?? string.Empty).WriteTo(writer);
		}
	}
}

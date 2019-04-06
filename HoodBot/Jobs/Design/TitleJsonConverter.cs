namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using RobinHood70.Robby;

	public class TitleJsonConverter : JsonConverter<Title>
	{
		private readonly Site site;

		public TitleJsonConverter(Site site) => this.site = site;

		public override Title ReadJson(JsonReader reader, Type objectType, Title existingValue, bool hasExistingValue, JsonSerializer serializer) => new Title(this.site, (string)reader.Value);

		public override void WriteJson(JsonWriter writer, Title value, JsonSerializer serializer) => JToken.FromObject(value.FullPageName).WriteTo(writer);
	}
}

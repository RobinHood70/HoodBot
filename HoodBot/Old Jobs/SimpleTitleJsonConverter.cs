namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class SimpleTitleJsonConverter : JsonConverter<Title>
	{
		private readonly Site site;

		public SimpleTitleJsonConverter(Site site)
		{
			this.site = site;
		}

		public override Title ReadJson(JsonReader reader, Type objectType, Title existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var title = (string)reader
				.NotNull()
				.Value
				.PropertyNotNull(nameof(reader), nameof(reader.Value));
			return TitleFactory.FromUnvalidated(this.site, title);
		}

		public override void WriteJson(JsonWriter writer, Title value, JsonSerializer serializer) => writer.NotNull().WriteValue(value.FullPageName());
	}
}

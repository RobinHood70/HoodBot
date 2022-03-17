namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;

	public class SimpleTitleJsonConverter : JsonConverter<SimpleTitle>
	{
		private readonly Site site;

		public SimpleTitleJsonConverter(Site site)
		{
			this.site = site;
		}

		public override SimpleTitle ReadJson(JsonReader reader, Type objectType, SimpleTitle? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var title = (string)reader
				.NotNull(nameof(reader))
				.Value
				.NotNull(nameof(reader), nameof(reader.Value));
			return Title.FromUnvalidated(this.site, title);
		}

		public override void WriteJson(JsonWriter writer, SimpleTitle? value, JsonSerializer serializer) => writer.NotNull(nameof(writer)).WriteValue(value.NotNull(nameof(value)).ToString() ?? string.Empty);
	}
}

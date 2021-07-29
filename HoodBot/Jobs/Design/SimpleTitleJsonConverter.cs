namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class SimpleTitleJsonConverter : JsonConverter<ISimpleTitle>
	{
		private readonly Site site;

		public SimpleTitleJsonConverter(Site site) => this.site = site;

		public override ISimpleTitle ReadJson(JsonReader reader, Type objectType, ISimpleTitle? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var title = (string)reader.NotNull(nameof(reader))
				.Value.NotNull(nameof(reader), nameof(reader.Value));
			return Title.FromName(this.site, title);
		}

		public override void WriteJson(JsonWriter writer, ISimpleTitle? value, JsonSerializer serializer) => writer.NotNull(nameof(writer)).WriteValue(value.NotNull(nameof(value)).ToString() ?? string.Empty);
	}
}

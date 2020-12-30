﻿namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using Newtonsoft.Json;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	public class SimpleTitleJsonConverter : JsonConverter<ISimpleTitle>
	{
		private readonly Site site;

		public SimpleTitleJsonConverter(Site site) => this.site = site;

		public override ISimpleTitle ReadJson(JsonReader reader, Type objectType, ISimpleTitle? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			ThrowNull(reader, nameof(reader));
			ThrowNull(reader.Value, nameof(reader), nameof(reader.Value));
			var title = (string)reader.Value;
			return Title.FromName(this.site, title);
		}

		public override void WriteJson(JsonWriter writer, ISimpleTitle? value, JsonSerializer serializer)
		{
			ThrowNull(writer, nameof(writer));
			ThrowNull(value, nameof(value));
			writer.WriteValue(value.ToString() ?? string.Empty);
		}
	}
}

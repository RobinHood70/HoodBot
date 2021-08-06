namespace RobinHood70.HoodBot.Design
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;

	internal static class JsonSubSetting<T>
		where T : IJsonSubSetting<T>, new()
	{
		public static T FromJson(JToken json)
		{
			T subSetting = new();
			subSetting.FromJson(json.NotNull(nameof(json)));

			return subSetting;
		}
	}
}
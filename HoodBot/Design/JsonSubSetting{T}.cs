namespace RobinHood70.HoodBot.Design
{
	using Newtonsoft.Json.Linq;
	using static RobinHood70.CommonCode.Globals;

	internal static class JsonSubSetting<T>
		where T : IJsonSubSetting<T>, new()
	{
		public static T FromJson(JToken json)
		{
			ThrowNull(json, nameof(json));
			var subSetting = new T();
			subSetting.FromJson(json);

			return subSetting;
		}
	}
}
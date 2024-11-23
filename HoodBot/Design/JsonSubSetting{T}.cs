namespace RobinHood70.HoodBot.Design;

using System;
using Newtonsoft.Json.Linq;

internal static class JsonSubSetting<T>
	where T : IJsonSubSetting<T>, new()
{
	public static T FromJson(JToken json)
	{
		ArgumentNullException.ThrowIfNull(json);
		T subSetting = new();
		subSetting.FromJson(json);

		return subSetting;
	}
}
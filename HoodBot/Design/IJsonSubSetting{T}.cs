namespace RobinHood70.HoodBot.Design;

using Newtonsoft.Json.Linq;

internal interface IJsonSubSetting<T>
	where T : new()
{
	#region Methods
	void FromJson(JToken json);

	JToken ToJson();
	#endregion
}
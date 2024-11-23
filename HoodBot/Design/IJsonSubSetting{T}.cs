namespace RobinHood70.HoodBot.Design;

using Newtonsoft.Json.Linq;

internal interface IJsonSubSetting<T>
	where T : new()
{
	#region Methods
	public void FromJson(JToken json);

	public JToken ToJson();
	#endregion
}
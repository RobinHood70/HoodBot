namespace RobinHood70.HoodBot.Design;

internal interface IJsonSettings<T> : IJsonSubSetting<T>
	where T : new()
{
	#region Properties
	string FileName { get; }
	#endregion
}
namespace RobinHood70.HoodBot.Design
{
	internal interface IJsonSettings<T> : IJsonSubSetting<T>
		where T : new()
	{
		#region Properties
		public string FileName { get; }
		#endregion
	}
}
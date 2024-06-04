namespace RobinHood70.HoodBot.ViewModels
{
	internal sealed class SettingsParameters(WikiInfoViewModel selectedItem)
	{
		#region Public Properties
		public WikiInfoViewModel SelectedItem { get; } = selectedItem;
		#endregion
	}
}
namespace RobinHood70.HoodBot.ViewModels
{
	internal sealed class SettingsParameters
	{
		public SettingsParameters(WikiInfoViewModel selectedItem)
		{
			this.SelectedItem = selectedItem;
		}

		public WikiInfoViewModel SelectedItem { get; }
	}
}
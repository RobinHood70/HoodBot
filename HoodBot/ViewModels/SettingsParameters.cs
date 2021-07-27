namespace RobinHood70.HoodBot.ViewModels
{
	using RobinHood70.WallE.Clients;

	internal sealed class SettingsParameters
	{
		public SettingsParameters(IMediaWikiClient client, WikiInfoViewModel selectedItem)
		{
			this.Client = client;
			this.SelectedItem = selectedItem;
		}

		public IMediaWikiClient Client { get; }

		public WikiInfoViewModel SelectedItem { get; }
	}
}

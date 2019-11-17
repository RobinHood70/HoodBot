namespace RobinHood70.HoodBot
{
	using System.Windows;
	using RobinHood70.HoodBot.ViewModel;
	using RobinHood70.WallE.Clients;

	/// <summary>Interaction logic for EditSettings.xaml.</summary>
	public partial class EditSettings : Window
	{
		public EditSettings(BotSettings botSettings, IMediaWikiClient client, WikiInfo? currentItem)
		{
			this.InitializeComponent();
			if (this.DataContext is SettingsViewModel view)
			{
				view.BotSettings = botSettings;
				view.Client = client;
				view.CurrentItem = currentItem;
			}
		}
	}
}
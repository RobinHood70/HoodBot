namespace RobinHood70.HoodBot.Views
{
	using System.Windows;
	using RobinHood70.HoodBot.ViewModels;
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
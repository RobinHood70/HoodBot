namespace RobinHood70.HoodBot
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using RobinHood70.HoodBot.ViewModel;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Clients;

	/// <summary>
	/// Interaction logic for EditWikiList.xaml.
	/// </summary>
	public partial class EditWikiList : Window
	{
		private WikiInfoViewModel viewModel;

		public EditWikiList(WikiInfoViewModel viewModel)
		{
			this.InitializeComponent();
			this.DataContext = viewModel;
			viewModel.SetPasswordBox(this.PasswordBox);
			this.viewModel = viewModel;
		}

		private void FillButton_Click(object sender, RoutedEventArgs e)
		{
			var wikiInfo = this.viewModel;
			var page = this.AnyPage.Text;
			if (string.IsNullOrWhiteSpace(page) && wikiInfo.Api != null)
			{
				page = wikiInfo.Api.ToString();
			}

			if (string.IsNullOrWhiteSpace(page))
			{
				return;
			}

			var client = new SimpleClient(Globals.ContactInfo, Globals.CookiesLocation);
			var uri = new Uri(page);
			var capabilities = new SiteCapabilities(client);
			capabilities.Get(uri);
			if (string.IsNullOrWhiteSpace(wikiInfo.DisplayName))
			{
				wikiInfo.DisplayName = capabilities.SiteName;
			}

			if (string.IsNullOrWhiteSpace(wikiInfo.Api?.OriginalString))
			{
				wikiInfo.Api = capabilities.Api;
			}

			if (string.IsNullOrWhiteSpace(wikiInfo.UserName))
			{
				wikiInfo.UserName = capabilities.CurrentUser;
			}

			if (wikiInfo.ReadThrottling == 0)
			{
				wikiInfo.ReadThrottling = capabilities.SupportsMaxLag ? 0 : 1000;
			}

			if (wikiInfo.WriteThrottling == 0)
			{
				wikiInfo.WriteThrottling = capabilities.SupportsMaxLag ? 0 : 5000;
			}
		}

		private void AddButton_Click(object sender, RoutedEventArgs e) => this.viewModel.Add();

		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) => this.viewModel.ChangePassword((sender as PasswordBox).Password);

		private void RemoveButton_Click(object sender, RoutedEventArgs e) => this.viewModel.RemoveCurrent();

		private void SaveButton_Click(object sender, RoutedEventArgs e) => this.viewModel.EndEdit();

		private void UndoButton_Click(object sender, RoutedEventArgs e) => this.viewModel.CancelEdit();

		private void WikiList_SelectionChanged(object sender, SelectionChangedEventArgs e) => this.viewModel.UpdateSelection((sender as ListBox).SelectedItem as WikiInfo);
	}
}
namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Command;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Clients;

	// TODO: Re-examine WikiInfo vs MaxLaggableWikiInfo. Need to handle it better.
	public class SettingsViewModel : ViewModelBase, IEditableObject
	{
		#region Fields
		private IMediaWikiClient? client;
		private WikiInfoViewModel? selectedItem;
		#endregion

		#region Constructors
		public SettingsViewModel() => this.MessengerInstance.Register<SettingsParameters>(this, this.Initialize);
		#endregion

		#region Public Commands
		public RelayCommand Add => new(this.NewWiki);

		public RelayCommand<string> AutoFill => new(this.Fill);

		public RelayCommand Remove => new(this.RemoveCurrent);

		public RelayCommand Save => new(this.EndEdit);

		public RelayCommand Undo => new(this.CancelEdit);
		#endregion

		#region Public Properties
		public WikiInfoViewModel? SelectedItem
		{
			get => this.selectedItem;
			set
			{
				if (value != this.selectedItem)
				{
					this.CancelEdit();
					this.Set(ref this.selectedItem, value);
					this.BeginEdit();
				}
			}
		}

		public UserSettings UserSettings { get; } = App.UserSettings;
		#endregion

		#region Public Methods

		public void BeginEdit() => this.SelectedItem?.BeginEdit();

		public void CancelEdit() => this.SelectedItem?.CancelEdit();

		public void EndEdit()
		{
			if (this.SelectedItem != null)
			{
				if (!this.SelectedItem.IsValid)
				{
					// Abort save if current wiki is invalid.
					MessageBox.Show(Resources.InvalidWikiInfo, Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				this.SelectedItem.EndEdit();
			}

			Settings.Save(App.UserSettings);
		}
		#endregion

		#region Private Methods
		private void Fill(string page)
		{
			if (this.SelectedItem is not WikiInfoViewModel selectedItem)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(page) && selectedItem.Api?.IsWellFormedOriginalString() == true)
			{
				page = selectedItem.Api.OriginalString;
			}

			if (string.IsNullOrWhiteSpace(page))
			{
				return;
			}

			var uri = new Uri(page);
			var capabilities = new SiteCapabilities(this.client);
			if (capabilities.Get(uri))
			{
				var noName = string.IsNullOrWhiteSpace(selectedItem.DisplayName);
				if (string.IsNullOrWhiteSpace(selectedItem.DisplayName))
				{
					selectedItem.DisplayName = capabilities.SiteName;
				}

				if (string.IsNullOrWhiteSpace(selectedItem.Api?.OriginalString))
				{
					selectedItem.Api = capabilities.Api;
				}

				if (string.IsNullOrWhiteSpace(selectedItem.UserName))
				{
					selectedItem.UserName = capabilities.CurrentUser;
				}

				if (noName || selectedItem.ReadThrottling == 0)
				{
					selectedItem.ReadThrottling = capabilities.SupportsMaxLag ? 0 : 1000;
				}

				if (noName || selectedItem.WriteThrottling == 0)
				{
					selectedItem.WriteThrottling = capabilities.SupportsMaxLag ? 0 : 5000;
				}
			}
		}

		private void NewWiki()
		{
			var retval = new WikiInfoViewModel();
			App.UserSettings.Wikis.Add(retval);
			this.SelectedItem = retval;
		}

		private void RemoveCurrent()
		{
			if (this.SelectedItem != null)
			{
				App.UserSettings.RemoveWiki(this.SelectedItem);
			}
		}

		private void Initialize(SettingsParameters parameters)
		{
			// TODO: main.Client no longer guaranteed to be non-null...likely not to be, in fact.
			this.MessengerInstance.Unregister(this);
			this.client = parameters.Client;
			this.SelectedItem = parameters.SelectedItem;
		}
		#endregion
	}
}
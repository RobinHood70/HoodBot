namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using GalaSoft.MvvmLight;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Clients;
	using static RobinHood70.CommonCode.Globals;

	// TODO: Re-examine WikiInfo vs MaxLaggableWikiInfo. Need to handle it better.
	public class SettingsViewModel : ViewModelBase, IEditableObject
	{
		#region Fields
		private Uri? api;
		private string? botDataFolder;
		private BotSettings? botSettings;
		private WikiInfo? currentItem;
		private string? displayName;
		private string? password;
		private int readThrottling;
		private string? siteClassIdentifier;
		private string? userName;
		private int writeThrottling;
		#endregion

		#region Public Properties
		public RelayCommand Add => new RelayCommand(() => this.CurrentItem = null);

		public Uri? Api
		{
			get => this.api;
			set => this.Set(ref this.api, value, nameof(this.Api));
		}

		public RelayCommand<string> AutoFill => new RelayCommand<string>(this.Fill);

		public string? BotDataFolder
		{
			get => this.botDataFolder;
			set => this.Set(ref this.botDataFolder, value, nameof(this.BotDataFolder));
		}

		public BotSettings? BotSettings
		{
			get => this.botSettings;
			set => this.Set(ref this.botSettings, value ?? throw ArgumentNull(nameof(value)), nameof(this.BotSettings));
		}

		public IMediaWikiClient? Client { get; set; }

		public WikiInfo? CurrentItem
		{
			get => this.currentItem;
			set
			{
				this.Set(ref this.currentItem, value, nameof(this.CurrentItem));
				this.UpdateSelection(value);
			}
		}

		public string? DisplayName
		{
			get => this.displayName;
			set => this.Set(ref this.displayName, value, nameof(this.DisplayName));
		}

		public string? Password
		{
			get => this.password;
			set => this.Set(ref this.password, value, nameof(this.Password));
		}

		public int ReadThrottling
		{
			get => this.readThrottling;
			set => this.Set(ref this.readThrottling, value, nameof(this.ReadThrottling));
		}

		public RelayCommand Remove => new RelayCommand(() => this.RemoveCurrent());

		public RelayCommand Save => new RelayCommand(() => this.EndEdit());

		public string? SiteClassIdentifier
		{
			get => this.siteClassIdentifier;
			set => this.Set(ref this.siteClassIdentifier, value, nameof(this.SiteClassIdentifier));
		}

		public RelayCommand Undo => new RelayCommand(() => this.CancelEdit());

		public string? UserName
		{
			get => this.userName;
			set => this.Set(ref this.userName, value, nameof(this.UserName));
		}

		public int WriteThrottling
		{
			get => this.writeThrottling;
			set => this.Set(ref this.writeThrottling, value, nameof(this.WriteThrottling));
		}
		#endregion

		#region Public Methods
		public void BeginEdit()
		{
		}

		public void CancelEdit()
		{
			ThrowNull(this.BotSettings, nameof(SettingsViewModel), nameof(this.BotSettings));
			this.UpdateSelection(this.BotSettings.GetCurrentItem());
		}

		public void EndEdit()
		{
			ThrowNull(this.BotSettings, nameof(SettingsViewModel), nameof(this.BotSettings));
			if (string.IsNullOrWhiteSpace(this.DisplayName) || !(this.Api?.IsWellFormedOriginalString() ?? false))
			{
				MessageBox.Show(Resources.InvalidWikiInfo, Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var wikiInfo = this.CurrentItem ?? this.NewWiki();
			wikiInfo.Api = this.Api;
			wikiInfo.DisplayName = this.DisplayName;
			wikiInfo.Password = this.Password;
			wikiInfo.ReadThrottling = this.ReadThrottling;
			wikiInfo.SiteClassIdentifier = this.SiteClassIdentifier;
			wikiInfo.WriteThrottling = this.WriteThrottling;
			wikiInfo.UserName = this.UserName;

			this.BotSettings.Save();
		}
		#endregion

		#region Internal Methods
		internal void UpdateSelection(WikiInfo? wikiInfo)
		{
			ThrowNull(this.BotSettings, nameof(SettingsViewModel), nameof(this.BotSettings));
			if (wikiInfo == null)
			{
				this.Api = null;
				this.DisplayName = null;
				this.Password = null;
				this.ReadThrottling = 0;
				this.SiteClassIdentifier = null;
				this.WriteThrottling = 0;
				this.UserName = null;
			}
			else
			{
				this.Api = wikiInfo.Api;
				this.DisplayName = wikiInfo.DisplayName;
				this.Password = wikiInfo.Password;
				this.ReadThrottling = wikiInfo.ReadThrottling;
				this.SiteClassIdentifier = wikiInfo.SiteClassIdentifier;
				this.WriteThrottling = wikiInfo.WriteThrottling;
				this.UserName = wikiInfo.UserName;
			}

			this.BotSettings.UpdateCurrentWiki(wikiInfo);
		}
		#endregion

		#region Private Methods
		private void Fill(string page)
		{
			if (string.IsNullOrWhiteSpace(page) && this.Api != null && this.Api.IsWellFormedOriginalString())
			{
				page = this.Api.OriginalString;
			}

			if (string.IsNullOrWhiteSpace(page))
			{
				return;
			}

			var uri = new Uri(page);
			var capabilities = new SiteCapabilities(this.Client);
			if (capabilities.Get(uri))
			{
				var noName = string.IsNullOrWhiteSpace(this.DisplayName);
				if (string.IsNullOrWhiteSpace(this.DisplayName))
				{
					this.DisplayName = capabilities.SiteName;
				}

				if (string.IsNullOrWhiteSpace(this.Api?.OriginalString))
				{
					this.Api = capabilities.Api;
				}

				if (string.IsNullOrWhiteSpace(this.UserName))
				{
					this.UserName = capabilities.CurrentUser;
				}

				if (noName || this.ReadThrottling == 0)
				{
					this.ReadThrottling = capabilities.SupportsMaxLag ? 0 : 1000;
				}

				if (noName || this.WriteThrottling == 0)
				{
					this.WriteThrottling = capabilities.SupportsMaxLag ? 0 : 5000;
				}
			}
		}

		private WikiInfo NewWiki()
		{
			ThrowNull(this.BotSettings, nameof(SettingsViewModel), nameof(this.BotSettings));
			var retval = new WikiInfo();
			this.BotSettings.Wikis.Add(retval);
			this.currentItem = retval;

			return retval;
		}

		private void RemoveCurrent()
		{
			if (this.CurrentItem != null)
			{
				ThrowNull(this.BotSettings, nameof(SettingsViewModel), nameof(this.BotSettings));
				this.BotSettings.RemoveWiki(this.CurrentItem);
			}
		}
		#endregion
	}
}
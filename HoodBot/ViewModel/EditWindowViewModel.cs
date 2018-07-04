namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Clients;
	using static RobinHood70.HoodBot.Properties.Resources;

	public class EditWindowViewModel : Notifier, IWikiInfo, IEditableObject
	{
		#region Fields
		private Uri api;
		private WikiInfo currentItem;
		private string displayName;
		private WikiList knownWikis;
		private string password;
		private int readThrottling;
		private string userName;
		private int writeThrottling;
		#endregion

		#region Public Properties
		public RelayCommand Add => new RelayCommand(() => this.CurrentItem = null);

		public Uri Api
		{
			get => this.api;
			set => this.Set(ref this.api, value, nameof(this.Api));
		}

		public RelayCommand<string> AutoFill => new RelayCommand<string>(this.Fill);

		public IMediaWikiClient Client { get; set; }

		public WikiInfo CurrentItem
		{
			get => this.currentItem;
			set
			{
				this.Set(ref this.currentItem, value, nameof(this.CurrentItem));
				this.UpdateSelection(value);
			}
		}

		public string DisplayName
		{
			get => this.displayName;
			set => this.Set(ref this.displayName, value, nameof(this.DisplayName));
		}

		public WikiList KnownWikis
		{
			get => this.knownWikis;
			set => this.Set(ref this.knownWikis, value, nameof(this.KnownWikis));
		}

		public string Password
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

		public RelayCommand Undo => new RelayCommand(() => this.CancelEdit());

		public string UserName
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

		public void CancelEdit() => this.UpdateSelection(null);

		public void EndEdit()
		{
			if (string.IsNullOrWhiteSpace(this.DisplayName) || !(this.Api?.IsWellFormedOriginalString() ?? false))
			{
				MessageBox.Show(InvalidWikiInfo, Error, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var wikiInfo = this.CurrentItem;
			if (wikiInfo == null)
			{
				wikiInfo = new WikiInfo();
				this.KnownWikis.Wikis.Add(wikiInfo);
			}

			CopyWikiInfo(this, wikiInfo);
			this.currentItem = wikiInfo;
			this.KnownWikis.Save();
		}
		#endregion

		#region Internal Methods
		internal void UpdateSelection(WikiInfo wikiInfo)
		{
			if (wikiInfo == null)
			{
				this.Api = null;
				this.DisplayName = null;
				this.Password = null;
				this.ReadThrottling = 0;
				this.WriteThrottling = 0;
				this.UserName = null;
			}
			else
			{
				CopyWikiInfo(wikiInfo, this);
			}

			this.KnownWikis.UpdateLastSelected(wikiInfo);
		}
		#endregion

		#region Private Methods
		private static void CopyWikiInfo(IWikiInfo from, IWikiInfo to)
		{
			to.Api = from.Api;
			to.DisplayName = from.DisplayName;
			to.Password = from.Password;
			to.ReadThrottling = from.ReadThrottling;
			to.WriteThrottling = from.WriteThrottling;
			to.UserName = from.UserName;
		}

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

		private void RemoveCurrent()
		{
			if (this.CurrentItem != null)
			{
				this.KnownWikis.Remove(this.CurrentItem);
			}
		}
		#endregion
	}
}
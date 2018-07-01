namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Controls;
	using Newtonsoft.Json;
	using static HoodBot.Properties.Resources;

	[Serializable]
	public class WikiInfoViewModel : IWikiInfo, IEditableObject, INotifyPropertyChanged
	{
		#region Fields
		[NonSerialized]
		private PasswordBox passwordBox;

		[NonSerialized]
		private string currentValue;

		[NonSerialized]
		private Uri api;

		[NonSerialized]
		private string displayName;

		[NonSerialized]
		private string pwd;

		[NonSerialized]
		private int readThrottling;

		[NonSerialized]
		private string userName;

		[NonSerialized]
		private int writeThrottling;
		#endregion

		#region Public Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Public Properties
		public ObservableCollection<WikiInfo> AllWikis { get; } = new ObservableCollection<WikiInfo>();

		public string CurrentValue
		{
			get => this.currentValue;
			set => this.Set(ref this.currentValue, value);
		}

		[JsonIgnore]
		public Uri Api
		{
			get => this.api;
			set => this.Set(ref this.api, value);
		}

		[JsonIgnore]
		public string DisplayName
		{
			get => this.displayName;
			set => this.Set(ref this.displayName, value);
		}

		[JsonIgnore]
		public string Password
		{
			get => this.pwd;
			set
			{
				if (this.Set(ref this.pwd, value))
				{
					this.passwordBox.Password = value;
				}
			}
		}

		[JsonIgnore]
		public int ReadThrottling
		{
			get => this.readThrottling;
			set => this.Set(ref this.readThrottling, value);
		}

		[JsonIgnore]
		public string UserName
		{
			get => this.userName;
			set => this.Set(ref this.userName, value);
		}

		[JsonIgnore]
		public int WriteThrottling
		{
			get => this.writeThrottling;
			set => this.Set(ref this.writeThrottling, value);
		}
		#endregion

		#region Public Static Methods
		public static WikiInfoViewModel Load()
		{
			string input;
			try
			{
				input = File.ReadAllText(Globals.WikiListLocation);
				return JsonConvert.DeserializeObject<WikiInfoViewModel>(input);
			}
			catch (DirectoryNotFoundException)
			{
			}
			catch (FileNotFoundException)
			{
			}

			return new WikiInfoViewModel();
		}
		#endregion

		#region Public Methods
		public void Add() => this.CurrentValue = null;

		public void BeginEdit()
		{
		}

		public void CancelEdit() => this.UpdateSelection(this.FindCurrent());

		// Deliberately bypasses property, allowing internal value and displayed value to be different.
		public void ChangePassword(string password) => this.pwd = password;

		public void EndEdit()
		{
			if (string.IsNullOrWhiteSpace(this.DisplayName) || !(this.Api?.IsWellFormedOriginalString() ?? false))
			{
				MessageBox.Show(InvalidWikiInfo, Error, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var wikiInfo = this.FindCurrent();
			if (wikiInfo == null)
			{
				wikiInfo = new WikiInfo();
				this.AllWikis.Add(wikiInfo);
			}

			CopyWikiInfo(this, wikiInfo);
			this.CurrentValue = this.DisplayName;
			this.Save();
		}

		public WikiInfo FindCurrent()
		{
			// Simply iterates the collection because ObservableCollection supports nothing else and the collection should always be small enough for speed not to be an issue.
			WikiInfo retval = null;
			if (this.AllWikis.Count > 0)
			{
				foreach (var item in this.AllWikis)
				{
					if (item.DisplayName == this.CurrentValue)
					{
						retval = item;
						break;
					}
				}
			}

			return retval;
		}

		public void RemoveCurrent()
		{
			var wikiInfo = this.FindCurrent();
			if (wikiInfo != null)
			{
				this.AllWikis.Remove(wikiInfo);
			}

			this.Save();
		}

		public void Save()
		{
			var output = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
			File.WriteAllText(Globals.WikiListLocation, output);
		}
		#endregion

		#region Internal Methods
		internal void SetPasswordBox(PasswordBox control) => this.passwordBox = control;

		internal void UpdateSelection(IWikiInfo wikiInfo)
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
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

		private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			var retval = !EqualityComparer<T>.Default.Equals(field, value);
			if (retval)
			{
				field = value;
				this.OnPropertyChanged(propertyName);
			}

			return retval;
		}
		#endregion
	}
}
namespace RobinHood70.HoodBot.Models
{
	using System;
	using System.ComponentModel;
	using GalaSoft.MvvmLight;
	using Newtonsoft.Json.Linq;
	using static RobinHood70.CommonCode.Globals;

	public class WikiInfo : ObservableObject, IEditableObject
	{
		#region Static Fields
		// Yes, this key is hard-coded. There are more secure ways of doing it, but for now, this will suffice - user would have to specifically share their settings file in order to have passwords decrypted, and even then, they won't be displayed on-screen...they'd only be available in code.
		private static readonly TextEncrypter Encrypter = new TextEncrypter("¡ʇᴉ ǝʇɐɔsnɟqO");
		#endregion

		#region Fields
		private Uri? api;
		private string? displayName;
		private int maxLag;
		private string? password;
		private int readThrottling;
		private string? siteClassIdentifier;
		private string? userName;
		private int writeThrottling;
		private WikiInfo? saved;
		#endregion

		#region Constructors
		public WikiInfo()
		{
		}

		public WikiInfo(JToken node)
		{
			ThrowNull(node, nameof(node));
			this.Api = (Uri?)node[nameof(this.Api)];
			this.DisplayName = (string?)node[nameof(this.DisplayName)];
			this.MaxLag = (int?)node[nameof(this.MaxLag)] ?? 5;
			this.ReadThrottling = (int?)node[nameof(this.ReadThrottling)] ?? 0;
			this.SiteClassIdentifier = (string?)node[nameof(this.SiteClassIdentifier)];
			this.UserName = (string?)node[nameof(this.UserName)];
			this.WriteThrottling = (int?)node[nameof(this.WriteThrottling)] ?? 0;
			var password = (string?)node[nameof(this.Password)];
			if (password != null)
			{
				this.Password = Encrypter.Decrypt(password);
			}
		}
		#endregion

		#region Public Properties
		public Uri? Api
		{
			get => this.api;
			set => this.Set(ref this.api, value);
		}

		public string? DisplayName
		{
			get => this.displayName;
			set => this.Set(ref this.displayName, value);
		}

		public int MaxLag
		{
			get => this.maxLag;
			set => this.Set(ref this.maxLag, value);
		}

		public string? Password
		{
			get => this.password;
			set => this.Set(ref this.password, value);
		}

		public int ReadThrottling
		{
			get => this.readThrottling;
			set => this.Set(ref this.readThrottling, value);
		}

		public string? SiteClassIdentifier
		{
			get => this.siteClassIdentifier;
			set => this.Set(ref this.siteClassIdentifier, value);
		}

		public string? UserName
		{
			get => this.userName;
			set => this.Set(ref this.userName, value);
		}

		public int WriteThrottling
		{
			get => this.writeThrottling;
			set => this.Set(ref this.writeThrottling, value);
		}

		public void BeginEdit()
		{
			this.saved = new WikiInfo();
			CopyInfo(this, this.saved);
		}

		public void CancelEdit() => CopyInfo(this.saved, this);

		public void EndEdit()
		{
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayName ?? this.GetType().Name;
		#endregion

		#region Internal Methods
		internal JToken ToJson()
		{
			var password = Encrypter.Encrypt(this.Password ?? string.Empty);
			var json = new JObject()
			{
				new JProperty(nameof(this.Api), this.Api),
				new JProperty(nameof(this.DisplayName), this.DisplayName),
				new JProperty(nameof(this.MaxLag), this.MaxLag),
				new JProperty(nameof(this.Password), password),
				new JProperty(nameof(this.ReadThrottling), this.ReadThrottling),
				new JProperty(nameof(this.SiteClassIdentifier), this.SiteClassIdentifier),
				new JProperty(nameof(this.UserName), this.UserName),
				new JProperty(nameof(this.WriteThrottling), this.WriteThrottling),
			};

			return json;
		}
		#endregion

		#region Private Static Methods
		private static void CopyInfo(WikiInfo? from, WikiInfo? to)
		{
			if (from != null && to != null)
			{
				to.api = from.api;
				to.displayName = from.displayName;
				to.maxLag = from.maxLag;
				to.password = from.password;
				to.readThrottling = from.readThrottling;
				to.siteClassIdentifier = from.siteClassIdentifier;
				to.userName = from.userName;
				to.writeThrottling = from.writeThrottling;
			}
		}
		#endregion
	}
}
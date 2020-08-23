namespace RobinHood70.HoodBot.Models
{
	using System;
	using GalaSoft.MvvmLight;

	public class WikiInfo : ObservableObject
	{
		#region Public Properties
		private Uri? api;
		private string? displayName;
		private int maxLag;
		private string? password;
		private int readThrottling;
		private string? siteClassIdentifier;
		private string? userName;
		private int writeThrottling;
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

		[Newtonsoft.Json.JsonConverter(typeof(EncryptingJsonConverter), "¡ʇᴉ ǝʇɐɔsnɟqO")]
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
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayName ?? this.GetType().Name;
		#endregion
	}
}
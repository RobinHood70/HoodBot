namespace RobinHood70.HoodBot.Models
{
	using System;
	using Newtonsoft.Json.Linq;
	using static RobinHood70.CommonCode.Globals;

	public sealed class WikiInfo
	{
		#region Static Fields
		// Yes, this key is hard-coded. There are more secure ways of doing it, but for now, this will suffice - user would have to specifically share their settings file in order to have passwords decrypted, and even then, they won't be displayed on-screen...they'd only be available in code.
		private static readonly TextEncrypter Encrypter = new TextEncrypter("¡ʇᴉ ǝʇɐɔsnɟqO");
		#endregion

		#region Constructors
		public WikiInfo()
		{
		}

		internal WikiInfo(JToken node)
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
		public Uri? Api { get; set; }

		public string? DisplayName { get; set; }

		public bool IsValid => !string.IsNullOrWhiteSpace(this.DisplayName) && this.Api?.IsWellFormedOriginalString() == true;

		public int MaxLag { get; set; }

		public string? Password { get; set; }

		public int ReadThrottling { get; set; }

		public string? SiteClassIdentifier { get; set; }

		public string? UserName { get; set; }

		public int WriteThrottling { get; set; }
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
	}
}
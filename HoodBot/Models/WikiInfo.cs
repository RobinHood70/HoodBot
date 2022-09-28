namespace RobinHood70.HoodBot.Models
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;

	// Note: password security for this class is minimal and fairly easily reversed. If this is a concern, it's recommended not to store passowrds and instead enter them manually every time.
	public sealed class WikiInfo : IJsonSubSetting<WikiInfo>
	{
		#region Public Constants
		public const int DefaultMaxLag = 5;
		#endregion

		#region Public Properties
		public Uri? Api { get; set; }

		public string? DisplayName { get; set; }

		public bool IsValid => !string.IsNullOrWhiteSpace(this.DisplayName) && this.Api?.IsWellFormedOriginalString() == true;

		public string? LogPage { get; set; }

		public int? MaxLag { get; set; }

		public string? Password { get; set; }

		public int? ReadThrottling { get; set; }

		public string? ResultsPage { get; set; }

		public string? SiteClassIdentifier { get; set; }

		public string? UserName { get; set; }

		public int? WriteThrottling { get; set; }
		#endregion

		#region Public Methods
		public void FromJson(JToken json)
		{
			var api = (string?)json.NotNull()[nameof(this.Api)] ?? "/";
			try
			{
				this.Api = new Uri(api, UriKind.RelativeOrAbsolute);
			}
			catch (UriFormatException)
			{
				this.Api = new Uri("/", UriKind.Relative);
			}

			this.DisplayName = (string?)json[nameof(this.DisplayName)];
			this.LogPage = (string?)json[nameof(this.LogPage)];
			this.MaxLag = (int?)json[nameof(this.MaxLag)];
			var password = (string?)json[nameof(this.Password)];
			if (password is not null)
			{
				this.Password = Settings.Encrypter.Decrypt(password);
			}

			this.ReadThrottling = (int?)json[nameof(this.ReadThrottling)];
			this.ResultsPage = (string?)json[nameof(this.ResultsPage)];
			this.SiteClassIdentifier = (string?)json[nameof(this.SiteClassIdentifier)];
			this.UserName = (string?)json[nameof(this.UserName)];
			this.WriteThrottling = (int?)json[nameof(this.WriteThrottling)];
		}

		#endregion

		#region Public Methods
		public JToken ToJson()
		{
			JObject json = new()
			{
				{ nameof(this.Api), new JValue(this.Api) },
				{ nameof(this.DisplayName), new JValue(this.DisplayName) }
			};

			if (this.Password != null)
			{
				// This one is added separately due to ensure that Password is defined prior to sending it to Encrypt(). Plus, AddToJson chokes on this.
				json.Add(nameof(this.Password), new JValue(Settings.Encrypter.Encrypt(this.Password)));
			}

			AddToJson(json, nameof(this.LogPage), this.LogPage, true);
			AddToJson(json, nameof(this.MaxLag), this.MaxLag, this.MaxLag > 0);
			AddToJson(json, nameof(this.ReadThrottling), this.ReadThrottling, this.ReadThrottling > 0);
			AddToJson(json, nameof(this.ResultsPage), this.ResultsPage, this.ResultsPage is not null);
			AddToJson(json, nameof(this.SiteClassIdentifier), this.SiteClassIdentifier, this.SiteClassIdentifier is not null);
			AddToJson(json, nameof(this.UserName), this.UserName, this.UserName is not null);
			AddToJson(json, nameof(this.WriteThrottling), this.WriteThrottling, this.WriteThrottling > 0);

			return json;

			static void AddToJson(JObject json, string name, object? property, bool condition)
			{
				if (condition && property is not null)
				{
					var value = new JValue(property);
					json.Add(name, value);
				}
			}
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayName ?? this.GetType().Name;
		#endregion

	}
}
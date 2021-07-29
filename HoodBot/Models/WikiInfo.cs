namespace RobinHood70.HoodBot.Models
{
	using System;
	using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;

	// Note: password security for this class is minimal and fairly easily reversed. If this is a concern, it's recommended not to store passowrds and instead enter them manually every time.
	public sealed class WikiInfo : IJsonSubSetting<WikiInfo>
	{
		#region Private Constants
		private const int DefaultMaxLag = 5;
		#endregion

		#region Public Properties
		public Uri? Api { get; set; }

		public string? DisplayName { get; set; }

		public bool IsValid => !string.IsNullOrWhiteSpace(this.DisplayName) && this.Api?.IsWellFormedOriginalString() == true;

		public string? LogPage { get; set; }

		public int MaxLag { get; set; } = DefaultMaxLag;

		public string? Password { get; set; }

		public int ReadThrottling { get; set; }

		public string? ResultsPage { get; set; }

		public string? SiteClassIdentifier { get; set; }

		public string? UserName { get; set; }

		public int WriteThrottling { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayName ?? this.GetType().Name;
		#endregion

		#region Public Methods
		public void FromJson(JToken json)
		{
			var api = (string?)json.NotNull(nameof(json))[nameof(this.Api)] ?? "/";
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
			this.MaxLag = (int?)json[nameof(this.MaxLag)] ?? DefaultMaxLag;
			var password = (string?)json[nameof(this.Password)];
			if (password != null)
			{
				this.Password = Settings.Encrypter.Decrypt(password);
			}

			this.ReadThrottling = (int?)json[nameof(this.ReadThrottling)] ?? 0;
			this.ResultsPage = (string?)json[nameof(this.ResultsPage)];
			this.SiteClassIdentifier = (string?)json[nameof(this.SiteClassIdentifier)];
			this.UserName = (string?)json[nameof(this.UserName)];
			this.WriteThrottling = (int?)json[nameof(this.WriteThrottling)] ?? 0;
		}

		public JToken ToJson()
		{
			var json = new JObject
			{
				{ nameof(this.Api), new JValue(this.Api) },
				{ nameof(this.DisplayName), new JValue(this.DisplayName) }
			};

			AddToJson(nameof(this.LogPage), this.LogPage, null);
			AddToJson(nameof(this.MaxLag), this.MaxLag, DefaultMaxLag);
			if (this.Password != null)
			{
				json.Add(nameof(this.Password), new JValue(Settings.Encrypter.Encrypt(this.Password)));
			}

			AddToJson(nameof(this.ReadThrottling), this.ReadThrottling, 0);
			AddToJson(nameof(this.ResultsPage), this.ResultsPage, null);
			AddToJson(nameof(this.SiteClassIdentifier), this.SiteClassIdentifier, null);
			AddToJson(nameof(this.UserName), this.UserName, null);
			AddToJson(nameof(this.WriteThrottling), this.WriteThrottling, 0);

			return json;

			void AddToJson(string name, object? property, object? defaultValue)
			{
				if (property is null ? !(defaultValue is null) : !property.Equals(defaultValue))
				{
					json.Add(name, new JValue(property));
				}
			}
		}
		#endregion
	}
}
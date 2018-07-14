namespace RobinHood70.HoodBot
{
	using System;

	public class WikiInfo : IWikiInfo
	{
		#region Public Properties
		public Uri Api { get; set; }

		public string DisplayName { get; set; }

		// TODO: Add this to forms and saved data. Hard-coded for now.
		public int MaxLag { get; set; } = 5;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
		[Newtonsoft.Json.JsonConverter(typeof(EncryptingJsonConverter), "¡ʇᴉ ǝʇɐɔsnɟqO")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
		public string Password { get; set; }

		public int ReadThrottling { get; set; }

		public string UserName { get; set; }

		public int WriteThrottling { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayName;
		#endregion
	}
}

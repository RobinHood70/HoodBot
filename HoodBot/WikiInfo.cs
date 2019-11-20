namespace RobinHood70.HoodBot
{
	using System;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;
	using static RobinHood70.WikiCommon.Globals;

	public class WikiInfo
	{
		#region Public Properties
		public Uri? Api { get; set; }

		public string? DisplayName { get; set; }

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
		[Newtonsoft.Json.JsonConverter(typeof(EncryptingJsonConverter), "¡ʇᴉ ǝʇɐɔsnɟqO")]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
		public string? Password { get; set; }

		public int ReadThrottling { get; set; }

		public string? SiteClassIdentifier { get; set; }

		public string? UserName { get; set; }

		public int WriteThrottling { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayName ?? this.GetType().Name;
		#endregion

		#region Public Virtual Methods
		public virtual IWikiAbstractionLayer GetAbstractionLayer(IMediaWikiClient client) => this.Api == null ? throw PropertyNull(nameof(WikiInfo), nameof(this.Api)) : new WikiAbstractionLayer(client, this.Api);
		#endregion
	}
}
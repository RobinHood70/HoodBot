﻿namespace RobinHood70.HoodBot
{
	using System;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;

	public class WikiInfo
	{
		#region Public Properties
		public Uri Api { get; set; }

		public string DisplayName { get; set; }

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

		#region Public Virtual Methods
		public virtual IWikiAbstractionLayer GetAbstractionLayer(IMediaWikiClient client) => new WikiAbstractionLayer(client, this.Api);
		#endregion
	}
}
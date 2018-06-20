#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum InterwikiMapFlags
	{
		None = 0,
		ExtraLanguageLink = 1,
		Local = 1 << 1,
		LocalInterwiki = 1 << 2,
		ProtocolRelative = 1 << 3,
		TransclusionAllowed = 1 << 4
	}
	#endregion

	public class InterwikiMapItem
	{
		#region Public Properties
		public string ApiUrl { get; set; }

		public string Language { get; set; }

		public string LinkText { get; set; }

		public InterwikiMapFlags Flags { get; set; }

		public string Prefix { get; set; }

		public string SiteName { get; set; }

		public string Url { get; set; }

		public string WikiId { get; set; }
		#endregion
	}
}

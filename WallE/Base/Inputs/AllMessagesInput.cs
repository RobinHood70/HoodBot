#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	#region Public Enumerations
	[Flags]
	public enum AllMessagesProperties
	{
		None = 0,
		Default = 1
	}
	#endregion

	// Not a list in the API query sense, but acts similar to one, so grouped as one.
	public class AllMessagesInput
	{
		#region Public Properties
		public IEnumerable<string>? Arguments { get; set; }

		public bool EnableParser { get; set; }

		public string? EnableParserTitle { get; set; }

		public string? Filter { get; set; }

		public Filter FilterModified { get; set; }

		public bool IncludeLocal { get; set; }

		public string? LanguageCode { get; set; }

		public string? MessageFrom { get; set; }

		public IEnumerable<string>? Messages { get; set; }

		public string? MessageTo { get; set; }

		public bool NoContent { get; set; }

		public string? Prefix { get; set; }

		public AllMessagesProperties Properties { get; set; }
		#endregion
	}
}
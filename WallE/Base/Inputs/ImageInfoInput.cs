#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class ImageInfoInput : IPropertyInput, ILimitableInput
	{
		#region Public Properties
		public DateTime? End { get; set; }

		public IEnumerable<string>? ExtendedMetadataFilter { get; set; }

		public string? ExtendedMetadataLanguage { get; set; }

		public bool ExtendedMetadataMultilanguage { get; set; }

		public int Limit { get; set; } = 1;

		public bool LocalOnly { get; set; }

		public int MaxItems { get; set; }

		/// <summary>Gets or sets the metadata version to use.</summary>
		/// <value>The numeric metadata version. Set to 0 to default to latest value (not emitted in query).</value>
		public int MetadataVersion { get; set; }

		public ImageProperties Properties { get; set; }

		public DateTime? Start { get; set; }

		public int UrlHeight { get; set; }

		public string? UrlParameter { get; set; }

		public int UrlWidth { get; set; }
		#endregion
	}
}

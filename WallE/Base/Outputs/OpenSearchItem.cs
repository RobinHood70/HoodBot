#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	public class OpenSearchItem : IApiTitleOptional
	{
		#region Constructors
		internal OpenSearchItem(string? title, string? description, Uri? uri)
		{
			this.Title = title;
			this.Description = description;
			this.Uri = uri;
		}
		#endregion

		#region Public Properties
		public string? Description { get; }

		public string? Title { get; }

		public Uri? Uri { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? Globals.Unknown;
		#endregion
	}
}

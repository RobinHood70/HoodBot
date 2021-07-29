#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum ImageRotateFlags
	{
		None = 0,
		Invalid = 1,
		Missing = 1 << 1
	}
	#endregion

	public class ImageRotateItem : IApiTitle
	{
		#region Constructors
		internal ImageRotateItem(int ns, string title, long pageId, IReadOnlyList<WarningsItem> errorMessage, string? result, ImageRotateFlags flags)
		{
			this.Namespace = ns;
			this.FullPageName = title;
			this.PageId = pageId;
			this.ErrorMessage = errorMessage;
			this.Result = result;
			this.Flags = flags;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<WarningsItem>? ErrorMessage { get; }

		public int Namespace { get; }

		public long PageId { get; }

		public ImageRotateFlags Flags { get; }

		public string? Result { get; }

		public string FullPageName { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.FullPageName;
		#endregion
	}
}

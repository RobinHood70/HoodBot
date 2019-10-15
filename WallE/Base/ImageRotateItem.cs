#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum ImageRotateFlags
	{
		None = 0,
		Invalid = 1,
		Missing = 1 << 1
	}
	#endregion

	public class ImageRotateItem : ITitle
	{
		#region Constructors
		public ImageRotateItem(int ns, string title, long pageId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<WarningsItem>? ErrorMessage { get; set; }

		public int Namespace { get; }

		public long PageId { get; }

		public ImageRotateFlags Flags { get; set; }

		public string? Result { get; set; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}

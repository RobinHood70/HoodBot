#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	[Flags]
	public enum EditFlags
	{
		None = 0,
		New = 1,
		NoChange = 1 << 1
	}

	public class EditResult
	{
		#region Public Properties
		public string ContentModel { get; set; }

		public EditFlags Flags { get; set; }

		public long NewRevisionId { get; set; }

		public DateTime? NewTimestamp { get; set; }

		public long OldRevisionId { get; set; }

		public long PageId { get; set; }

		public string Result { get; set; }

		public string Title { get; set; }
		#endregion
	}
}

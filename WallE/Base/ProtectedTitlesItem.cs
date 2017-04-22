#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class ProtectedTitlesItem : ITitleOnly
	{
		#region Public Properties
		public string Comment { get; set; }

		public DateTime? Expiry { get; set; }

		public string Level { get; set; }

		public int? Namespace { get; set; }

		public string ParsedComment { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Title { get; set; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}

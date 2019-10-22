#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using static RobinHood70.WikiCommon.Globals;

	// Note that FromId refers to a different page than Namespace and Title.
	public class AllLinksItem : ITitleOptional
	{
		#region Public Constructors
		internal AllLinksItem(int? ns, string? title, long fromId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.FromId = fromId;
		}
		#endregion

		#region Public Properties
		public long FromId { get; }

		public int? Namespace { get; }

		public string? Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? NoTitle;
		#endregion
	}
}

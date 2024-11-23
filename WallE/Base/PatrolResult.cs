#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class PatrolResult : IApiTitle
{
	#region Constructors
	internal PatrolResult(int ns, string title, long rcId)
	{
		this.Namespace = ns;
		this.Title = title;
		this.RecentChangesId = rcId;
	}
	#endregion

	#region Public Properties
	public int Namespace { get; }

	public long RecentChangesId { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}
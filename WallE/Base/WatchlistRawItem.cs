#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class WatchlistRawItem : IApiTitle
{
	#region Constructors
	internal WatchlistRawItem(int ns, string title, bool changed)
	{
		this.Namespace = ns;
		this.Title = title;
		this.Changed = changed;
	}
	#endregion

	#region Public Properties
	public bool Changed { get; }

	public int Namespace { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}
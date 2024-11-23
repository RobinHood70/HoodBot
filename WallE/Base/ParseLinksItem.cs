#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class ParseLinksItem(int ns, string title, bool exists) : IApiTitle
{
	#region Public Properties
	public bool Exists { get; } = exists;

	public int Namespace { get; } = ns;

	public string Title { get; } = title;
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}
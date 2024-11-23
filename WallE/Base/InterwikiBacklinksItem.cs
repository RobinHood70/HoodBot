#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class InterwikiBacklinksItem : IApiTitle
{
	#region Constructors
	internal InterwikiBacklinksItem(int ns, string title, long pageId, string? iwPrefix, string? iwTitle, bool isRedirect)
	{
		this.Namespace = ns;
		this.Title = title;
		this.PageId = pageId;
		this.InterwikiPrefix = iwPrefix;
		this.InterwikiTitle = iwTitle;
		this.IsRedirect = isRedirect;
	}
	#endregion

	#region Public Properties
	public string? InterwikiPrefix { get; }

	public string? InterwikiTitle { get; }

	public bool IsRedirect { get; }

	public int Namespace { get; }

	public long PageId { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.WikiCommon;

public class LanguageBacklinksItem : IApiTitle
{
	#region Constructors
	internal LanguageBacklinksItem(int ns, string title, long pageId, bool isRedirect, string? langCode, string? langTitle)
	{
		this.Namespace = ns;
		this.Title = title;
		this.PageId = pageId;
		this.IsRedirect = isRedirect;
		this.LanguageCode = langCode;
		this.LanguageTitle = langTitle;
	}
	#endregion

	#region Public Properties
	public bool IsRedirect { get; }

	public string? LanguageCode { get; }

	public string? LanguageTitle { get; }

	public int Namespace { get; }

	public long PageId { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}
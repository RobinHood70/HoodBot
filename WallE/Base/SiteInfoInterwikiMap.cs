#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

#region Public Enumerations
[Flags]
public enum InterwikiMapFlags
{
	None = 0,
	ExtraLanguageLink = 1,
	Local = 1 << 1,
	LocalInterwiki = 1 << 2,
	ProtocolRelative = 1 << 3,
	TransclusionAllowed = 1 << 4
}
#endregion

public class SiteInfoInterwikiMap
{
	#region Constructors
	internal SiteInfoInterwikiMap(string prefix, string url, string? apiUrl, InterwikiMapFlags flags, string? language, string? linkText, string? siteName, string? wikiId)
	{
		this.Prefix = prefix;
		this.Url = url;
		this.ApiUrl = apiUrl;
		this.Flags = flags;
		this.Language = language;
		this.LinkText = linkText;
		this.SiteName = siteName;
		this.WikiId = wikiId;
	}
	#endregion

	#region Public Properties
	public string? ApiUrl { get; }

	public string? Language { get; }

	public string? LinkText { get; }

	public InterwikiMapFlags Flags { get; }

	public string Prefix { get; }

	public string? SiteName { get; }

	public string Url { get; }

	public string? WikiId { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Prefix;
	#endregion
}
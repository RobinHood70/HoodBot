#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

public class SiteInfoGeneral
{
	#region Constructors
	internal SiteInfoGeneral(string articlePath, string basePage, string dbType, string dbVersion, IReadOnlyList<string> externalImages, string fallback8BitEncoding, List<string> fallbackLanguages, string? favicon, SiteInfoFlags flags, string generator, string? gitBranch, string? gitHash, string? hhvmVersion, IReadOnlyDictionary<string, ImageLimitsItem> imageLimits, string language, string? legalTitleChars, string? linkPrefix, string? linkPrefixCharset, string? linkTrail, string? logo, string mainPage, long maxUploadSize, string phpSapi, string phpVersion, string? readOnlyReason, long revision, string script, string scriptPath, string server, string? serverName, string siteName, IReadOnlyDictionary<string, int> thumbLimits, DateTime time, TimeSpan timeOffset, string timeZone, string? variantArticlePath, IReadOnlyList<string> variants, string wikiId)
	{
		this.ArticlePath = articlePath;
		this.BasePage = basePage;
		this.DbType = dbType;
		this.DbVersion = dbVersion;
		this.ExternalImages = externalImages;
		this.Fallback8BitEncoding = fallback8BitEncoding;
		this.FallbackLanguages = fallbackLanguages;
		this.Favicon = favicon;
		this.Flags = flags;
		this.Generator = generator;
		this.GitBranch = gitBranch;
		this.GitHash = gitHash;
		this.HhvmVersion = hhvmVersion;
		this.ImageLimits = imageLimits;
		this.Language = language;
		this.LegalTitleChars = legalTitleChars;
		this.LinkPrefix = linkPrefix;
		this.LinkPrefixCharset = linkPrefixCharset;
		this.LinkTrail = linkTrail;
		this.Logo = logo;
		this.MainPage = mainPage;
		this.MaxUploadSize = maxUploadSize;
		this.PhpSapi = phpSapi;
		this.PhpVersion = phpVersion;
		this.ReadOnlyReason = readOnlyReason;
		this.Revision = revision;
		this.Script = script;
		this.ScriptPath = scriptPath;
		this.Server = server;
		this.ServerName = serverName;
		this.SiteName = siteName;
		this.ThumbLimits = thumbLimits;
		this.Time = time;
		this.TimeOffset = timeOffset;
		this.TimeZone = timeZone;
		this.VariantArticlePath = variantArticlePath;
		this.Variants = variants;
		this.WikiId = wikiId;
	}
	#endregion

	#region Public Properties
	public string ArticlePath { get; }

	public string BasePage { get; }

	public string DbType { get; }

	public string DbVersion { get; }

	public IReadOnlyList<string?> ExternalImages { get; }

	public IReadOnlyList<string?> FallbackLanguages { get; }

	public string Fallback8BitEncoding { get; }

	public string? Favicon { get; }

	public SiteInfoFlags Flags { get; }

	public string Generator { get; }

	public string? GitBranch { get; }

	public string? GitHash { get; }

	public string? HhvmVersion { get; }

	public IReadOnlyDictionary<string, ImageLimitsItem> ImageLimits { get; }

	public string Language { get; }

	public string? LegalTitleChars { get; }

	public string? LinkPrefix { get; }

	public string? LinkPrefixCharset { get; }

	public string? LinkTrail { get; }

	public string? Logo { get; }

	public string MainPage { get; }

	public long MaxUploadSize { get; }

	public string PhpSapi { get; }

	public string PhpVersion { get; }

	public string? ReadOnlyReason { get; }

	public long Revision { get; }

	public string Script { get; }

	public string ScriptPath { get; }

	public string Server { get; }

	public string? ServerName { get; }

	public string SiteName { get; }

	public IReadOnlyDictionary<string, int> ThumbLimits { get; }

	public DateTime Time { get; }

	public TimeSpan TimeOffset { get; }

	public string TimeZone { get; }

	public string? VariantArticlePath { get; }

	/// <summary>Gets the list of language variants.</summary>
	/// <value>The list of language variants.</value>
	/// <remarks>Language names are <i>not</i> included, even when returned by the wiki. This is to keep this collection compatible with the related Fallback collection and because of the unlikelihood of ever needing that information in a bot.</remarks>
	public IReadOnlyList<string> Variants { get; }

	public string WikiId { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.SiteName;
	#endregion
}
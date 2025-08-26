namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

#region Public Enumerations
public enum EsoFileTypes
{
	Books,
	Collectibles,
	CrownCrates,
	GameUIArt,
	Icons,
	Lang,
	LoadScreens,
	Maps,
	Quests,
	SplitIcons,
	Store,
	TreasureMaps,
	TreeIcons,
	Tribute,
	Tutorial,
}
#endregion

public static class EsoFiles
{
	#region Private Constants
	private const string FileVersionsName = "FileVersions.csv";
	#endregion

	#region Public Properties
	public static IReadOnlyDictionary<EsoFileTypes, string> FileSystemNames { get; } = new Dictionary<EsoFileTypes, string>
	{
		[EsoFileTypes.Books] = "books",
		[EsoFileTypes.Collectibles] = "collectibles",
		[EsoFileTypes.CrownCrates] = "crowncrates",
		[EsoFileTypes.GameUIArt] = "gameuiart",
		[EsoFileTypes.Icons] = "icons",
		[EsoFileTypes.Lang] = "lang",
		[EsoFileTypes.LoadScreens] = "loadscreens",
		[EsoFileTypes.Maps] = "maps",
		[EsoFileTypes.Quests] = "quests",
		[EsoFileTypes.SplitIcons] = "spliticons",
		[EsoFileTypes.Store] = "store",
		[EsoFileTypes.TreasureMaps] = "treasuremaps",
		[EsoFileTypes.TreeIcons] = "treeicons",
		[EsoFileTypes.Tribute] = "tribute",
		[EsoFileTypes.Tutorial] = "tutorial",
	};
	#endregion

	#region Public Methods
	public static string CalculateHash(string fileName)
	{
		using var stream = File.OpenRead(fileName);
		return Globals.GetHash(stream, HashType.Sha1);
	}

	public static void DownloadEsoFile(this WikiJob job, EsoFileTypes fileType) => DownloadEsoFile(job, fileType, EsoLog.LatestDBUpdate(false));

	public static void DownloadEsoFile(this WikiJob job, EsoFileTypes fileType, EsoVersion desiredVersion)
	{
		var fileVersions = GetFileVersions();
		var fileName = FileSystemNames[fileType];

		if (fileVersions[fileName] == desiredVersion)
		{
			return;
		}

		var downloadPath = RemotePath(desiredVersion, EsoFileTypes.Icons);
		var localFile = fileName + ".zip";

		job.StatusWriteLine($"Updating local {fileName} file from {downloadPath}");
		job.Site.Download(downloadPath, localFile);

		job.StatusWriteLine($"Extracting {fileName}");
		var extractPath = LocalPath(fileType);
		ZipFile.ExtractToDirectory(localFile, extractPath, true);
	}

	public static IDictionary<string, EsoVersion> GetFileVersions()
	{
		var fullPath = Path.Join(LocalConfig.WikiIconsFolder, FileVersionsName);
		var csv = new CsvFile(fullPath);
		var retval = new SortedDictionary<string, EsoVersion>(StringComparer.Ordinal);
		foreach (var fileName in FileSystemNames.Values)
		{
			retval.Add(fileName, EsoVersion.Empty);
		}

		foreach (var row in csv.ReadRows())
		{
			var name = row[0];
			var version = row[1];
			retval[name] = new EsoVersion(version);
		}

		return retval;
	}

	public static IReadOnlyDictionary<string, FilePage> GetOriginalFiles(Site site) => GetOriginalFiles(site, PageModules.Default);

	public static IReadOnlyDictionary<string, FilePage> GetOriginalFiles(Site site, PageModules pageModules)
	{
		const string TemplateName = "Online File";

		ArgumentNullException.ThrowIfNull(site);
		var retval = new Dictionary<string, FilePage>(StringComparer.OrdinalIgnoreCase);

		var pages = new PageCollection(site, pageModules);
		pages.SetLimitations(LimitationType.OnlyAllow);
		pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn, true, Filter.Exclude, MediaWikiNamespaces.File);
		foreach (var page in pages)
		{
			var parser = new Robby.Parser.SiteParser(page);
			if (parser.FindTemplate(TemplateName) is not ITemplateNode template)
			{
				Debug.WriteLine("Template not found: " + page.Title.FullPageName()); // This should not be possible.
				continue;
			}

			if (template.GetValue("originalfile") is not string originalFileName)
			{
				Debug.WriteLine("Missing originalfileName parameter: " + page.Title.FullPageName()); // Possible, but undesirable.
				continue;
			}

			if (!retval.TryAdd(originalFileName, (FilePage)page))
			{
				Debug.WriteLine("Duplicate files: " + page.Title.PageName + " and " + retval[originalFileName].Title.PageName);
			}
		}

		return retval;
	}

	public static string LocalPath(EsoFileTypes fileType) => Path.Combine(LocalConfig.EsoUIArtFolder, FileSystemNames[fileType]);

	public static string RemotePath(EsoVersion patchVersion) => $"https://esofiles.uesp.net/update-{patchVersion}/";

	public static string RemotePath(EsoVersion patchVersion, EsoFileTypes fileType) =>
		RemotePath(patchVersion) + FileSystemNames[fileType] + ".zip";

	public static string SanitizeFileName(string paramValue)
	{
		paramValue = paramValue.Replace("<br>", string.Empty, StringComparison.OrdinalIgnoreCase);
		if (paramValue.Length > 0 && paramValue[0] == '/')
		{
			paramValue = paramValue[1..];
		}

		if (paramValue.StartsWith("esoui/art/", StringComparison.OrdinalIgnoreCase))
		{
			paramValue = paramValue[10..];
		}

		var split = paramValue.Split('.', 2);
		var ext = split.Length > 1 ? split[1] : string.Empty;
		if (string.Equals(ext, "png", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ext, "dds", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ext, "jpg", StringComparison.OrdinalIgnoreCase))
		{
			paramValue = split[0];
		}

		return paramValue;
	}

	public static void SaveFileVersions(IDictionary<string, EsoVersion> versions)
	{
		ArgumentNullException.ThrowIfNull(versions);
		var fullPath = Path.Join(LocalConfig.WikiIconsFolder, FileVersionsName);
		var csv = new CsvFile(fullPath);
		foreach (var fileName in FileSystemNames)
		{
			versions.TryAdd(fileName.Value, EsoVersion.Empty);
		}

		foreach (var row in versions)
		{
			csv.Add(row.Key, row.Value.ToString());
		}
	}
	#endregion
}
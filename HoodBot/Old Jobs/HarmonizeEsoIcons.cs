namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("Harmonize Online Files", "|ESO")]
internal sealed class HarmonizeEsoIcons(JobManager jobManager) : MovePagesJob(jobManager, false)
{
	#region Protected Override Methods
	protected override void BeforeLoadPages()
	{
		EsoFiles.DownloadEsoFile(this, EsoFileTypes.Icons);
		this.StatusWriteLine("Checking duplicate icons");
		CheckForDuplicateIcons();
		base.BeforeLoadPages();
	}

	protected override void PopulateMoves()
	{
		this.StatusWriteLine("Loading icons from wiki");
		var filePages = this.GetPages();
		var byHash = GetHashDict(filePages);
		var byName = GetNameDict(byHash);
		foreach (var localFile in byName.Keys)
		{
			var fullName = Path.Combine(LocalConfig.BotDataFolder, localFile);
			var sha1 = EsoFiles.CalculateHash(fullName);
		}
	}
	#endregion

	#region Private Static Methods
	private static void CheckForDuplicateIcons()
	{
		var checksums = EsoFiles.GetIconChecksums();
		foreach (var (_, value) in checksums)
		{
			if (value.Count > 1)
			{
				Debug.WriteLine("Duplicate Icons:");
				foreach (var fileName in value)
				{
					Debug.Write(" https://esoicons.uesp.net/esoui/art/icons" + fileName.Replace('\\', '/'));
				}

				Debug.WriteLine(string.Empty);
			}
		}
	}

	private static Dictionary<string, OnlineFile> GetHashDict(PageCollection filePages)
	{
		var byHash = new Dictionary<string, OnlineFile>(StringComparer.Ordinal);
		foreach (var filePage in filePages.Cast<FilePage>())
		{
			var parser = new SiteParser(filePage);
			if (parser.FindTemplate("Online File") is not ITemplateNode template)
			{
				continue;
			}

			var hash = filePage.LatestFileRevision?.Sha1 ?? throw new InvalidOperationException("No Sha1");
			if (!byHash.TryGetValue(hash, out var onFileInfo))
			{
				onFileInfo = new OnlineFile();
				byHash.Add(hash, onFileInfo);
			}

			onFileInfo.MergeInfo(filePage, template);
		}

		return byHash;
	}

	private static Dictionary<string, OnlineFile> GetNameDict(Dictionary<string, OnlineFile> byHash)
	{
		var byName = new Dictionary<string, OnlineFile>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in byHash.Values)
		{
			foreach (var fileName in file.OriginalFiles)
			{
				byName.Add(fileName, file);
			}
		}

		return byName;
	}
	#endregion

	#region Private Methods
	private PageCollection GetPages()
	{
		var loadOptions = new PageLoadOptions(PageModules.Default | PageModules.FileInfo, false)
		{
			FileRevisionCount = 500
		};
		var filePages = new PageCollection(this.Site, loadOptions);
		filePages.SetLimitations(LimitationType.OnlyAllow, MediaWikiNamespaces.File);
		//// filePages.GetBacklinks("Template:Online File", BacklinksTypes.EmbeddedIn);
		filePages.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "ON-icon-h"); // FOR TESTING PURPOSES

		return filePages;
	}

	#endregion
}
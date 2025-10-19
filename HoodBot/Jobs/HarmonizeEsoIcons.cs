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

	#region Private Classes
	private sealed class OnlineFile
	{
		#region Public Properties
		public DateTime CreationTime { get; private set; } = DateTime.MinValue;

		public string? Description { get; private set; }

		public bool NoSummary { get; private set; }

		public SortedSet<string> OriginalFiles { get; } = [];

		public List<Title> Renames { get; } = [];

		public Title? Title { get; private set; }

		public SortedSet<(string ItemType, string Text)> Uses { get; } = [];
		#endregion

		#region Public Methods
		public void MergeInfo(FilePage page, ITemplateNode template)
		{
			foreach (var paramSet in template.ParameterCluster(2))
			{
				var itemType = paramSet[0].ToRaw();
				var text = paramSet[1].ToRaw();
				this.Uses.Add((itemType, text));
			}

			if (page.FileRevisions.Count > 1)
			{
				Debug.WriteLine(page.Title.ToString() + ": " + page.FileRevisions.Count.ToStringInvariant());
			}

			var creationTime = DateTime.MaxValue;
			foreach (var fileRev in page.FileRevisions)
			{
				if (fileRev.Timestamp is DateTime revTimestamp && revTimestamp < creationTime)
				{
					creationTime = revTimestamp;
				}
			}

			if (creationTime == DateTime.MaxValue)
			{
				throw new InvalidOperationException("No file revisions, or creation time not found.");
			}

			this.CreationTime = creationTime;
			this.Description = template.GetValue("description");
			this.NoSummary = !string.IsNullOrWhiteSpace(template.GetValue("nosummary"));
			if (template.GetValue("originalfile")?.Split(TextArrays.Comma) is string[] origFiles)
			{
				this.OriginalFiles.AddRange(origFiles.Select(s => s.Trim()));
			}

			if (this.Title is not null)
			{
				this.Renames.Add(this.Title);
			}

			this.Title = page.Title;
		}
		#endregion
	}
	#endregion
}
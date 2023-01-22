namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.IO.Compression;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	internal sealed class EsoFurnishingUpload : WikiJob
	{
		#region Constructors
		[JobInfo("Furnishing Icon Upload", "ESO")]
		public EsoFurnishingUpload(JobManager jobManager)
			: base(jobManager, JobType.Write)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "ESO Furnishing Upload";
		#endregion

		#region Protected Override Methods

		protected override void Main()
		{
			var downloadPath = this.IconDownloadPath();
			var localFile = Path.Combine(LocalConfig.BotDataFolder, "icons.zip");
			var extractPath = LocalConfig.WikiIconsFolder;

			if (File.GetLastWriteTime(localFile) < (DateTime.Now - TimeSpan.FromDays(1)))
			{
				this.StatusWriteLine("Updating local icons file");
				this.Site.Download(downloadPath, localFile);

				this.StatusWriteLine("Extracting icons");
				ZipFile.ExtractToDirectory(localFile, extractPath, true);
			}

			var site = (UespSite)this.Site;
			var pages = site.CreateMetaPageCollection(PageModules.None, false, "collectible", "icon", "id");
			pages.GetBacklinks("Template:Online Furnishing Summary", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

			var items = new SortedList<string, IconInfo>(StringComparer.Ordinal);
			this.GetPages(pages, items, false);
			this.GetPages(pages, items, true);

			var existingTitles = new TitleCollection(this.Site);
			foreach (var item in items)
			{
				if (string.IsNullOrWhiteSpace(item.Value.IconName))
				{
					Debug.WriteLine("Empty: " + item.Key);
				}
				else
				{
					existingTitles.Add(item.Value.IconName);
				}
			}

			var existing = existingTitles.Load(PageModules.Info);
			existing.RemoveExists(false);

			this.ProgressMaximum = items.Count;
			foreach (var item in items)
			{
				var iconInfo = item.Value;
				if (iconInfo.LocalIcon != null && !existing.Contains(iconInfo.IconName))
				{
					var pageText =
						"== Summary ==\n" +
						$"Original file: {iconInfo.LocalIcon}\n" +
						"\n" +
						"== Licensing ==\n" +
						"{{Zenimage}}" +
						"[[Category:Online-Icons-Furnishings]]";
					var wikiIconName = iconInfo.IconName.Replace("\"", string.Empty, StringComparison.Ordinal);
					site.Upload(Path.Combine(LocalConfig.WikiIconsFolder, iconInfo.LocalIcon), wikiIconName, "Upload furnishing icon", pageText);
				}

				this.Progress++;
			}
		}

		private void GetPages(PageCollection pages, SortedList<string, IconInfo> items, bool collectibleFilter)
		{
			var retvalIds = new Dictionary<long, IconInfo>();
			foreach (var page in pages)
			{
				if (page is VariablesPage varPage && varPage.GetVariable("id") is string idText)
				{
					if (collectibleFilter == (varPage.GetVariable("collectible") is not null))
					{
						var id = long.Parse(idText, this.Site.Culture);
						var toName = Title.ToLabelName(page.PageName);
						var icon = varPage.GetVariable("icon");
						if (string.IsNullOrEmpty(icon))
						{
							icon = Furnishing.IconName(toName);
						}

						var iconInfo = new IconInfo(id, "File:" + icon);
						if (icon.Contains('"', StringComparison.Ordinal))
						{
							this.Warn("Quote in title: " + icon);
						}

						if (items.TryAdd(toName, iconInfo))
						{
							retvalIds.Add(id, iconInfo);
						}
						else
						{
							Debug.WriteLine($"Duplicate ID: {id}. Pages: {items[toName]} => {page.PageName}");
						}
					}
				}
			}

			var query = collectibleFilter
				? "SELECT id, icon FROM collectibles WHERE id"
				: "SELECT CAST(itemId AS SIGNED INT) id, icon FROM minedItemSummary WHERE itemId";
			query += " IN(" + string.Join(",", retvalIds.Keys) + ") AND icon != '/esoui/art/icons/icon_missing.dds'";
			var dbItems = EsoLog.Database.RunQuery(query);
			foreach (var row in dbItems)
			{
				var icon = (string)row["icon"];
				icon = icon
					.Replace("/esoui/art/icons/", string.Empty, StringComparison.Ordinal)
					.Replace(".dds", ".png", StringComparison.Ordinal);
				var id = (long)row["id"];
				retvalIds[id].LocalIcon = icon;
			}
		}
		#endregion

		#region Private Records
		private sealed record IconInfo(long Id, string IconName)
		{
			public string? LocalIcon { get; set; }
		}
		#endregion
	}
}

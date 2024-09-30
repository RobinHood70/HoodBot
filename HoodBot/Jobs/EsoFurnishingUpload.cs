namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Furnishing Icon Upload", "ESO")]
	internal sealed class EsoFurnishingUpload(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
	{
		#region Public Override Properties
		public override string LogName => "ESO Furnishing Upload";
		#endregion

		#region Protected Override Methods

		protected override void Main()
		{
			var patchVersion = EsoLog.LatestDBUpdate(false);
			this.GetIcons(patchVersion.Text, false);
			PageCollection pages = this.GetPages();
			var items = new SortedList<string, IconInfo>(StringComparer.Ordinal);
			this.ParsePageInfo(pages, items, false);
			this.ParsePageInfo(pages, items, true);

			var existingTitles = new TitleCollection(this.Site);
			foreach (var item in items)
			{
				if (string.IsNullOrWhiteSpace(item.Value.IconName))
				{
					Debug.WriteLine("Empty: " + item.Key);
				}
				else
				{
					existingTitles.TryAdd(item.Value.IconName);
				}
			}

			var existing = existingTitles.Load(PageModules.Info | PageModules.FileInfo);

			this.ProgressMaximum = items.Count;
			foreach (var item in items)
			{
				this.CheckFileAndUpload(existing, item.Value);
			}
		}
		#endregion

		#region Private Methods
		private void CheckFileAndUpload(PageCollection existing, IconInfo iconInfo)
		{
			if (iconInfo.LocalIcon != null)
			{
				var localFileName = Path.Combine(LocalConfig.WikiIconsFolder, iconInfo.LocalIcon);
				var wikiIconName = iconInfo.IconName.Replace("\"", string.Empty, StringComparison.Ordinal);
				var page = existing[wikiIconName];

				if (EsoSpace.ShouldUpload(localFileName, (FilePage)page))
				{
					var parser = new ContextualParser(page);
					_ = EsoSpace.FindOrCreateOnlineFile(parser, iconInfo.LocalIcon);
					parser.AddCategory("Online-Icons-Furnishings", true);
					parser.UpdatePage();
					this.Site.Upload(localFileName, wikiIconName, "Upload furnishing icon", page.Text, false);
				}
			}

			this.Progress++;
		}

		private PageCollection GetPages()
		{
			var uesp = (UespSite)this.Site;
			var pages = uesp.CreateMetaPageCollection(PageModules.None, false, "collectible", "icon", "id", "transcluded");
			pages.GetBacklinks("Template:Online Furnishing Summary", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);
			pages.Remove("Online:Alliance War Dog");
			pages.Remove("Online:Alliance War Dog (Dominion)");
			pages.Remove("Online:Alliance War Dog (Pact)");
			pages.Remove("Online:Alliance War Horse");
			pages.Remove("Online:Alliance War Horse (Dominion)");
			pages.Remove("Online:Alliance War Horse (Pact)");
			return pages;
		}

		private void ParsePageInfo(PageCollection pages, SortedList<string, IconInfo> items, bool collectibleFilter)
		{
			var retvalIds = new Dictionary<long, IconInfo>();
			var retvalDupes = new Dictionary<long, Page>();
			foreach (var page in pages)
			{
				if (page is VariablesPage varPage &&
					varPage.GetVariable("id") is string idText &&
					idText.Length > 0 &&
					collectibleFilter == (varPage.GetVariable("collectible") is not null))
				{
					var id = long.Parse(idText, this.Site.Culture);
					var icon = varPage.GetVariable("icon");
					if (string.IsNullOrEmpty(icon))
					{
						icon = Furnishing.IconName(page.Title.PageName);
					}

					var iconInfo = new IconInfo(id, "File:" + icon);
					if (icon.Contains('"', StringComparison.Ordinal))
					{
						this.Warn("Quote in title: " + icon);
					}

					items.Add(page.Title.PageName, iconInfo);
					if (retvalIds.TryAdd(id, iconInfo))
					{
						retvalDupes.Add(id, page);
					}
					else
					{
						Debug.WriteLine($"Duplicate id: {id} on {retvalDupes[id]} => {page.Title.PageName}");
					}
				}
			}

			var query = collectibleFilter
				? "SELECT id, icon FROM collectibles WHERE id"
				: "SELECT CAST(itemId AS SIGNED INT) id, icon FROM minedItemSummary WHERE itemId";
			query += " IN(" + string.Join(',', retvalIds.Keys) + ") AND icon != '/esoui/art/icons/icon_missing.dds'";
			var dbItems = EsoLog.EsoDb.RunQuery(query);
			foreach (var row in dbItems)
			{
				var icon = EsoLog.ConvertEncoding((string)row["icon"]);
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
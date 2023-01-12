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
	using RobinHood70.WikiCommon;

	internal sealed class EsoFurnishingUpload : WikiJob
	{
		#region Fields
		private static readonly string WikiIconFolder = Path.Combine(UespSite.GetBotDataFolder(), "icons");
		#endregion

		#region Constructors
		[JobInfo("Furnishing Icon Upload", "ESO")]
		public EsoFurnishingUpload(JobManager jobManager)
			: base(jobManager, JobType.Write)
		{
		}
		#endregion

		#region Protected Override Methods

		protected override void Main()
		{
			var site = (UespSite)this.Site;
			var pages = site.CreateMetaPageCollection(PageModules.None, false, "collectible", "icon", "id");
			pages.GetBacklinks("Template:Online Furnishing Summary", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);
			/* pages.Remove("Online:Aeonstone Formation");
			pages.Remove("Online:Armory Station");
			pages.Remove("Online:Blackwood Tapestry");
			pages.Remove("Online:Lady Garick's Sacred Shield");
			pages.Remove("Online:Sacred Hourglass of Alkosh");
			*/

			var items = new SortedList<string, IconInfo>(StringComparer.Ordinal);
			this.GetPages(pages, false, items);
			this.GetPages(pages, true, items);

			this.ProgressMaximum = items.Count;
			foreach (var item in items)
			{
				var iconInfo = item.Value;
				if (iconInfo.LocalIcon != null)
				{
					var pageText =
						"== Summary ==\n" +
						$"Original file: {iconInfo.LocalIcon}\n" +
						"\n" +
						"== Licensing ==\n" +
						"{{Zenimage}}" +
						"[[Category:Online-Icons-Furnishings]]";

					site.Upload(Path.Combine(WikiIconFolder, iconInfo.LocalIcon), iconInfo.IconName, "Upload furnishing icon", pageText);
				}

				this.Progress++;
			}
		}

		private void GetPages(PageCollection pages, bool collectibleFilter, SortedList<string, IconInfo> items)
		{
			var retvalIds = new Dictionary<long, IconInfo>();
			foreach (var page in pages)
			{
				if (page is VariablesPage varPage && varPage.GetVariable("id") is string idText)
				{
					if (collectibleFilter == varPage.GetVariable("collectible") is not null)
					{
						var id = long.Parse(idText, this.Site.Culture);
						var toName = Title.ToLabelName(page.PageName);
						var icon = varPage.GetVariable("icon") ?? Furnishing.IconName(toName);
						var iconInfo = new IconInfo(id, icon);
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

			var query = collectibleFilter ? "SELECT id, icon FROM collectibles WHERE id" : "SELECT CAST(itemId AS SIGNED INT) id, icon FROM minedItemSummary WHERE itemId";
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

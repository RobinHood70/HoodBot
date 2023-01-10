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

	internal sealed class EsoFurningUpload : WikiJob
	{
		#region Fields
		private static readonly string WikiIconFolder = Path.Combine(UespSite.GetBotDataFolder(), "icons");
		#endregion

		#region Constructors
		[JobInfo("Furnishing Icon Upload", "ESO")]
		public EsoFurningUpload(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var site = (UespSite)this.Site;
			var pages = site.CreateMetaPageCollection(PageModules.None, false, "id");
			pages.GetBacklinks("Template:Online Furnishing Summary", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);
			var ids = new Dictionary<long, string>(pages.Count);
			foreach (var page in pages)
			{
				if (page is VariablesPage varPage && varPage.GetVariable("id") is string idText)
				{
					var id = long.Parse(idText, this.Site.Culture);
					if (!ids.TryAdd(id, page.PageName))
					{
						Debug.WriteLine($"Duplicate ID: {id}. Original page: {ids[id]} => {page.PageName}");
					}
				}
			}

			var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
			var idLookup = new Dictionary<string, long>(StringComparer.Ordinal);
			var dbItems = EsoLog.Database.RunQuery("SELECT id, icon FROM collectibles WHERE icon != '/esoui/art/icons/icon_missing.dds' AND id IN(" + string.Join(",", ids.Keys) + ")");
			foreach (var row in dbItems)
			{
				var icon = (string)row["icon"];
				icon = icon
					.Replace("/esoui/art/icons/", string.Empty, StringComparison.Ordinal)
					.Replace(".dds", ".png", StringComparison.Ordinal);
				var id = (long)row["id"];
				idLookup.Add(icon, id);
				sorted.Add(ids[id], icon);
			}

			var skip = new HashSet<string>(StringComparer.Ordinal)
			{
					"Aeonstone Formation",
					"Armory Station",
					"Blackwood Tapestry",
					"Lady Garick's Sacred Shield",
					"Sacred Hourglass of Alkosh"
			};

			this.ProgressMaximum = sorted.Count;
			foreach (var item in sorted)
			{
				var fromName = Path.Combine(WikiIconFolder, item.Value);
				var toName = Title.ToLabelName(item.Key);
				if (!skip.Contains(toName))
				{
					var fullName = $"File:ON-icon-furnishing-{toName}.png";
					var pageText = "== Summary ==\n" +
						$"Original file: {item.Value}\n" +
						"Used for:\n" +
						$":Collectible: {{{{Item Link|{toName}|collectid={item.Key}}}}}\n" +
						"\n" +
						"[[Category:Online-Icons-Furnishings]]\n" +
						"== Licensing ==\n" +
						"{{Zenimage}}";

					site.Upload(fromName, fullName, "Upload furnishing icon", pageText);
				}

				this.Progress++;
			}
		}
		#endregion
	}
}

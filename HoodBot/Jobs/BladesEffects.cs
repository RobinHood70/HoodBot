namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.CommonCode;

	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class BladesEffects : EditJob
	{
		#region Static Fields
		private static readonly HashSet<string> IgnoreList = new(StringComparer.Ordinal)
		{
			"Black Green Smoke",
			"Continuous Frost Damage",
			"Continuous Poison Damage",
			"Continuous Untyped Damage",
			"Resist Hazards",
			"Templar Set Block Bonus",
			"Templar Set Burning Revenge",
			"Templar Set Continuous Fire Damage",
		};
		#endregion

		#region Constructors
		[JobInfo("Create Effects", "Blades|")]
		public BladesEffects(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create Effects Page";

		protected override void LoadPages()
		{
			var fileName = LocalConfig.BotDataSubPath("QuestLanguageDatabase.txt");
			Dictionary<string, string> translation = new(StringComparer.Ordinal);
			CsvFile langFile = new();
			langFile.Load(fileName, true);
			foreach (var entry in langFile)
			{
				translation.Add(entry["ID"], entry["en-US"]);
			}

			fileName = LocalConfig.BotDataSubPath("ItemPropertyList.txt");
			var lines = File.ReadAllLines(fileName);
			var codeLines = BladesCodeLine.Parse(lines);

			var entries = codeLines["_propertyList"]["size"];
			TitleCollection titles = new(this.Site);
			foreach (var entry in entries)
			{
				if (entry["_editorName"].Value is string pageName)
				{
					if (pageName.StartsWith("Material ", StringComparison.Ordinal))
					{
						// pageName = pageName[9..];
					}

					titles.AddRange(UespNamespaces.Blades, pageName);
					titles.AddRange(UespNamespaces.Blades, pageName + " (effect)");
				}
				else
				{
					throw new InvalidOperationException("_editorName does not exist!");
				}
			}

			var pages = titles.Load(PageModules.Default, true);
			foreach (var entry in entries)
			{
				var desc = entry["_description"]["_key"].Value ?? throw new InvalidOperationException();
				desc = translation[desc]
					.Replace("{0} minute", "<duration> minute", StringComparison.Ordinal)
					.Replace("{0} second", "<duration> second", StringComparison.Ordinal)
					.Replace("{1} second", "<duration> second", StringComparison.Ordinal)
					.Replace("{0}", "<magnitude>", StringComparison.Ordinal)
					.Replace("{1}", "<magnitude2>", StringComparison.Ordinal)
					.Replace("{2}", "<magnitude3>", StringComparison.Ordinal);

				if (entry["_editorName"].Value is not string pageName || IgnoreList.Contains(pageName))
				{
					continue;
				}

				var isMaterial = pageName.StartsWith("Material ", StringComparison.Ordinal);
				if (isMaterial)
				{
					// pageName = pageName[9..];
				}

				pageName = "Blades:" + pageName;
				var altPageName = pageName + " (effect)";
				var page = pages.GetMapped(pageName);
				if (page is null)
				{
					continue;
				}

				var altPage = pages.GetMapped(altPageName);
				if (altPage is not null &&
					(altPage.Exists ||
					(page.Exists && !page.Text.Contains("Effect Summary", StringComparison.Ordinal))))
				{
					page = altPage;
				}

				if (!page.Exists)
				{
					page.Text = isMaterial
						? $"#REDIRECT [[Blades:{page.Title.PageName[9..]}]] [[Category:Redirects from Alternate Names]]"
						: string.Concat("{{Trail|Effects}}{{Minimal}}\n{{Effect Summary\ntype=\nimage=\nsyntax=", desc, "\n|notrail=1\n}}\n{{Stub|Effect}}");

					this.Pages.Add(page);
				}
			}
		}

		protected override void PageLoaded(Page page)
		{
			// TODO: For now, LoadPages takes care of everything. Should be split out.
		}
		#endregion
	}
}
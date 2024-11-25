namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("Wrong Race")]
internal sealed class WrongRace(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Static Fields
	private static readonly Dictionary<string, string> RaceAlternates = new(System.StringComparer.OrdinalIgnoreCase)
	{
		["Altmer"] = "High Elf",
		["Bosmer"] = "Wood Elf",
		["Dunmer"] = "Dark Elf"
	};
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		var namesPages = new TitleCollection(
			this.Site,
			UespNamespaces.Lore,
			UespFunctions.LoreNames);
		namesPages.Remove("Lore:Daedra Names");
		namesPages.Remove("Lore:Names");
		var namePages = new PageCollection(this.Site, PageModules.Links);
		namePages.GetTitles(namesPages);
		var templates = new TitleCollection(this.Site, MediaWikiNamespaces.Template, "Basic NPC Summary", "Legends NPC Summary", "Lore People Summary", "NPC Summary", "Online NPC Summary", "Shadowkey NPC Summary");
		this.ProgressMaximum = namePages.Count;
		foreach (var page in namePages)
		{
			var wantedRace = page.Title.PageName.Split(TextArrays.Space)[0];
			var nameLinks = new TitleCollection(this.Site);
			foreach (var link in page.Links)
			{
				nameLinks.TryAdd(link);
			}

			nameLinks.Remove(namesPages);
			var nameBackLinkPages = new PageCollection(this.Site, PageModules.Info | PageModules.Revisions)
			{
				AllowRedirects = Filter.Exclude
			};

			nameBackLinkPages.GetTitles(nameLinks);
			foreach (var npcPage in nameBackLinkPages)
			{
				var parser = new SiteParser(npcPage);
				foreach (var template in parser.FindTemplates(templates))
				{
					var race = template.GetValue("race");
					var altRace = RaceAlternates.TryGetValue(wantedRace, out var race2) ? race2 : string.Empty;
					if (!race.OrdinalICEquals(wantedRace) &&
						!race.OrdinalICEquals(altRace))
					{
						Debug.WriteLine($"Lore race of {wantedRace} doesn't match {race} on " + npcPage.Title.FullPageName());
					}
				}
			}

			this.Progress++;
		}
	}
	#endregion
}
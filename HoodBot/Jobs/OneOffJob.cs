namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffJob : EditJob
	{
		private static readonly HashSet<string> TestFiles = new(StringComparer.Ordinal)
		{
			"File:ON-creature-Blue Oasis Dragon Frog.jpg",
			"File:ON-creature-Cockroach.jpg",
			"File:ON-creature-Dread Lich.jpg",
			"File:ON-creature-Spider Hatchling.jpg",
			"File:ON-creature-Tangerine Dragon Frog.jpg",
			"File:ON-creature-Thodundor of the Hill 02.jpg",
			"File:ON-creature-Winter Moth.jpg",
			"File:ON-interior-Arbogasque's Home 02.jpg",
			"File:ON-interior-Arbogasque's Home.jpg",
			"File:ON-interior-Enthonor's House.jpg",
			"File:ON-interior-Ramimilk 02.jpg",
			"File:ON-interior-Ramimilk 03.jpg",
			"File:ON-interior-Uveran Bank.jpg",
			"File:ON-interior-West Tower.jpg",
			"File:ON-npc-Conjured Reflection.jpg",
			"File:ON-npc-Constable Ebarah.jpg",
			"File:ON-npc-Crimson Knight.jpg",
			"File:ON-npc-Dremora Harstryl 02.jpg",
			"File:ON-npc-Fenteladir.jpg",
			"File:ON-npc-Guild Mage.jpg",
			"File:ON-npc-Highland Enforcer.jpg",
			"File:ON-npc-Imbued Corpse.jpg",
			"File:ON-npc-Jackdaw Bravo.jpg",
			"File:ON-npc-Lady Minara.jpg",
			"File:ON-npc-Manor Guest 03.jpg",
			"File:ON-npc-Moon-Sentinel Guardian.jpg",
			"File:ON-npc-Plague of Crows.jpg",
			"File:ON-npc-Root Guard.jpg",
			"File:ON-npc-Ruddy Fang Swashbuckler.jpg",
			"File:ON-npc-Sahban 02.jpg",
			"File:ON-npc-Scavenger Thunder-Smith.jpg",
			"File:ON-npc-Sharp Stick Ravager.jpg",
			"File:ON-npc-Sleeps-Beneath-Himself.jpg",
			"File:ON-npc-Worm Cult Assassin 02.jpg",
			"File:ON-npc-Zadazi 02.jpg",
			"File:ON-place-Dead-Water Wayshrine.jpg",
			"File:ON-place-Hatching Pools 02.jpg",
			"File:ON-place-Skeleton Camps.jpg",
			"File:ON-place-White Fall Giant Camp.jpg",
			"File:ON-quest-Death Among the Dead-Water 04.jpg"
		};

		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var loadOptions = new PageLoadOptions(PageModules.FileInfo)
			{
				FileRevisionCount = 1000
			};

			var pages = new PageCollection(this.Site, loadOptions);
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-place");
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-npc");
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-creature");
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-interior");
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-misc");
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-item");
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-object");
			pages.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-quest");

			var august2020 = new DateTime(2020, 8, 1);
			var sept2020 = new DateTime(2020, 9, 1);
			var titles = new TitleCollection(this.Site);
			foreach (var page in pages)
			{
				if (page is FilePage filePage &&
					filePage.FileRevisions.Count > 0 &&
					filePage.FileRevisions[filePage.FileRevisions.Count - 1] is var firstRev &&
					string.Equals(firstRev.User, "MolagBallet", StringComparison.Ordinal) &&
					firstRev.Timestamp < august2020)
				{
					var add = true;
					if (filePage.FileRevisions.Count > 1)
					{
						foreach (var rev in filePage.FileRevisions)
						{
							bool isMolags = string.Equals(rev.User, "MolagBallet", StringComparison.Ordinal);
							if (rev.Timestamp >= sept2020 && isMolags)
							{
								add = false;
							}
							else if (!isMolags)
							{
								this.StatusWriteLine($"{filePage.FullPageName} was updated by {rev.User} on {rev.Timestamp}, size: {rev.Width} x {rev.Height}");
							}
						}
					}

					if (add)
					{
						titles.Add(filePage);
					}
				}
			}

			this.Pages.GetTitles(titles);
			foreach (var page in this.Pages)
			{
				var parser = new ContextualParser(page);
				if ((parser.FindTemplate("CleanImage") ?? parser.FindTemplate("Cleanimage")) is not SiteTemplateNode templateNode)
				{
					var first = parser.Nodes[0];
					templateNode = (SiteTemplateNode)parser.Nodes.Factory.TemplateNodeFromParts("CleanImage");
					parser.Nodes.Insert(0, templateNode);
					if (first is IHeaderNode)
					{
						parser.Nodes.Insert(1, parser.Nodes.Factory.TextNode("\n"));
					}
					else if (first is not null)
					{
						Debug.WriteLine(page.FullPageName);
					}
				}

				if (templateNode.Find("res") is not IParameterNode resParam)
				{
					resParam = templateNode.Add("res", string.Empty);
				}

				var value = resParam.Value.ToValue();
				if (!value.Contains("too low", StringComparison.OrdinalIgnoreCase))
				{
					if (value.Length == 0)
					{
						resParam.Value.AddText("too low");
						page.Text = parser.ToRaw();
					}
				}
			}
		}

		protected override void Main() => this.SavePages("Add low-resolution tag, per MolagBallet");
		#endregion
	}
}
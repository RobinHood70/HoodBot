﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	internal sealed class EsoNpcs : EditJob
	{
		#region Fields
		private readonly bool updateMode;
		#endregion

		#region Constructors
		[JobInfo("Create Missing NPCs", "ESO Update")]
		public EsoNpcs(JobManager jobManager, [JobParameter(DefaultValue = false)] bool updateMode)
			: base(jobManager)
		{
			this.updateMode = updateMode;
			this.SetResultDescription("Existing ESO NPC pages");
		}
		#endregion

		#region Protected Override Properties
		public override string LogName => (this.updateMode ? "Update" : "Create missing") + " ESO NPC pages";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			// TODO: Update mode could load pages with no loc or {{huh}} via MetaTemplate variables.
			this.StatusWriteLine("Getting NPC data");
			var npcCollection = this.GetNpcPages();
			npcCollection.GetLocations();

			this.StatusWriteLine("Getting place data");
			var places = EsoSpace.GetPlaces(this.Site);
			var placeInfo = EsoSpace.PlaceInfo;
			npcCollection.ParseLocations(places);
			foreach (var npc in npcCollection)
			{
				npc.TrimPlaces();
			}

			if (this.updateMode)
			{
				foreach (var npc in npcCollection)
				{
					// TODO: We end up parsing the page twice with this design, one in FilterNpcsUpdate and once here. See if there's something that can be done to optimize this.
					if (npc.Page is Page page &&
						new ContextualParser(page) is var parser &&
						parser.FindSiteTemplate("Online NPC Summary") is ITemplateNode template)
					{
						UpdateLocations(npc, template, parser.Factory, placeInfo);
						parser.UpdatePage();
						this.Pages.Add(page);
					}
					else
					{
						throw new InvalidOperationException();
					}
				}
			}
			else
			{
				foreach (var npc in npcCollection)
				{
					if (npc.Page is Page page)
					{
						page.Text = NewPageText(npc, this.Site.Culture, placeInfo);
						page.SetMinimalStartTimestamp();
						this.Pages.Add(page);
					}
				}
			}
		}

		protected override void Main() => this.SavePages(this.LogName, false);
		#endregion

		#region Private Static Methods

		private static string NewPageText(NpcData npc, CultureInfo culture, IEnumerable<PlaceInfo> placeInfo)
		{
			List<(string?, string)> parameters = new()
			{
				("image", string.Empty),
				("imgdesc", string.Empty),
				("race", string.Empty),
				("gender", npc.GenderText),
				("difficulty", npc.Difficulty > 0 ? npc.Difficulty.ToString(culture) : string.Empty),
				("reaction", npc.Reaction),
				("pickpocket", npc.PickpocketDifficulty > 0 ? npc.PickpocketDifficultyText : string.Empty),
				("loottype", npc.LootType),
				("faction", string.Empty)
			};

			WikiNodeFactory factory = new();
			var template = factory.TemplateNodeFromParts("Online NPC Summary", true, parameters);
			UpdateLocations(npc, template, factory, placeInfo);

			return new StringBuilder()
				.Append("{{Minimal|NPC}}")
				.AppendLine(WikiTextVisitor.Raw(template))
				.AppendLine()
				.AppendLine("<!-- Instructions: Provide an initial sentence summarizing the NPC (race, job, where they live). Subsequent paragraphs provide additional information about the NPC, such as related NPCs, schedule, equipment, etc. Note that quest-specific information DOES NOT belong on this page, but instead goes on the appropriate quest page. Spoilers should be avoided.-->")
				.AppendLine("{{NewLeft}}")
				.AppendLine()
				.AppendLine("<!--Instructions: If this NPC is related to any quests, replace \"Quest Name\" with the quest's name.--><!--")
				.AppendLine("==Related Quests==")
				.AppendLine("* {{Quest Link|Quest Name}}")
				.AppendLine("--><!--Instructions: Add any miscellaneous notes about the NPC here, with a bullet for each note.--><!--")
				.AppendLine("==Notes==")
				.AppendLine("* Add note here")
				.AppendLine("--><!--Instructions: Add any bugs related to the NPC here using the format below.--><!--")
				.AppendLine("==Bugs==")
				.AppendLine("{{Bug|Bug description}}")
				.AppendLine("** Workaround")
				.AppendLine("-->")
				.AppendLine()
				.AppendLine("{{Stub|NPC}}")
				.ToString();
		}

		private static int NpcComparer((NpcData Npc, string Issue) x, (NpcData Npc, string Issue) y) => SimpleTitleComparer.Instance.Compare(x.Npc.Page, y.Npc.Page);

		private static string NpcToWikiText(NpcData npc)
		{
			var places = string.Join(", ", npc.Places);
			var unknownLocs = string.Join(", ", npc.UnknownLocations);
			var retval = $"\n* Name: {npc.Name}\n* Gender: {npc.GenderText}";
			if (!string.IsNullOrWhiteSpace(npc.LootType))
			{
				retval += $"\n* Loot Type: {npc.LootType}";
			}

			if (!string.IsNullOrWhiteSpace(places))
			{
				retval += $"\n* Known Locations: {places}";
			}

			if (!string.IsNullOrWhiteSpace(unknownLocs))
			{
				retval += $"\n* Unknown Locations: {unknownLocs}";
			}

			return retval;
		}

		private static void UpdateLocations(NpcData npc, ITemplateNode template, IWikiNodeFactory factory, IEnumerable<PlaceInfo> placeInfos)
		{
			foreach (var (placeType, paramName, _, variesCount) in placeInfos)
			{
				var placeText = npc.GetParameterValue(placeType, variesCount);
				InsertLocation(template, factory, paramName, placeText);
			}

			if (npc.UnknownLocations.Count > 0)
			{
				string locText;
				if (npc.UnknownLocations.Count < 10)
				{
					SortedSet<string> list = new(StringComparer.Ordinal);
					foreach (var place in npc.UnknownLocations)
					{
						list.Add(place.Key.TitleName);
					}

					locText = string.Join(", ", list);
				}
				else
				{
					locText = "Varies";
				}

				InsertLocation(template, factory, "loc", locText);
			}

			static void InsertLocation(ITemplateNode template, IWikiNodeFactory factory, string name, string locText)
			{
				if (locText.Length > 0)
				{
					locText += '\n';
					if (template.Find(name) is IParameterNode loc)
					{
						var value = loc.Value.ToValue().TrimEnd();
						if (!string.Equals(value, locText, StringComparison.Ordinal))
						{
							loc.SetValue(loc.IsNullOrWhitespace() ? locText : (value + ", " + locText), ParameterFormat.Copy);
						}
					}
					else
					{
						var index = template.FindIndex("race");
						template.Parameters.Insert(index, factory.ParameterNodeFromParts(name, locText));
					}
				}
			}
		}
		#endregion

		#region Private Methods
		private NpcCollection GetNpcPages()
		{
			Title NpcTitle(string pageName) => CreateTitle.FromUnvalidated(this.Site, UespNamespaces.Online, pageName);
			TitleCollection existingTitles = new(this.Site);
			existingTitles.GetCategoryMembers("Online-NPCs", CategoryMemberTypes.Page, false);
			existingTitles.GetCategoryMembers("Online-Creatures-All", CategoryMemberTypes.Page, false);
			var npcs = EsoLog.GetNpcs();
			TitleCollection checkTitles = new(this.Site);
			foreach (var npc in npcs)
			{
				checkTitles.Add(npc.Name);
			}

			PageCollection checkPages = new(this.Site, new PageLoadOptions(PageModules.Info | PageModules.Properties, true));
			checkPages.GetTitles(checkTitles);

			Dictionary<long, string> npcRenames = new();
			NpcCollection loadNpcs = new();
			TitleCollection loadTitles = new(this.Site);
			foreach (var npc in npcs)
			{
				var title = NpcTitle(npc.Name);
				if (checkPages.TitleMap.TryGetValue(npc.Name, out var redirect))
				{
					npcRenames.Add(npc.Id, redirect.PageName);
				}
				else if (checkPages.TryGetValue(title, out var page) && page.IsDisambiguation == true)
				{
					ContextualParser parser = new(page);
					foreach (var linkNode in parser.LinkNodes)
					{
						SiteLink disambig = SiteLink.FromLinkNode(this.Site, linkNode, false);
						if (existingTitles.TryGetValue(disambig, out var disambigPage))
						{
							npcRenames.Add(npc.Id, disambigPage.PageName);
							break;
						}
					}
				}

				if (existingTitles.Contains(title))
				{
					if (this.updateMode)
					{
						loadNpcs.Add(npc);
						loadTitles.Add(title);
					}
				}
				else
				{
					loadNpcs.Add(npc);
					loadTitles.Add(title);
				}
			}

			PageCollection loadPages = new(this.Site, new PageLoadOptions(PageModules.Default | PageModules.DeletedRevisions | PageModules.Properties, true));
			loadPages.GetTitles(loadTitles);
			loadPages.Sort();

			NpcCollection retval = new();
			List<(NpcData, string)> issues = new();
			foreach (var npc in loadNpcs)
			{
				if (!npcRenames.TryGetValue(npc.Id, out var npcName))
				{
					npcName = npc.Name;
				}

				if (loadPages.TryGetValue(NpcTitle(npcName), out var page))
				{
					npc.Page = page;
					var issue = page switch
					{
						Page when page.IsDisambiguation == true => "is a disambiguation with no clear NPC link",
						Page when page.IsRedirect => "is a redirect to a content page without an Online NPC Summary",
						Page when !this.updateMode && page.IsMissing && page.PreviouslyDeleted => "was previously deleted",
						_ => null
					};

					if (issue == null)
					{
						ContextualParser parsed = new(page);
						var template = parsed.FindSiteTemplate("Online NPC Summary");
						if (this.updateMode)
						{
							if (template?.Find("city").IsNullOrWhitespace() == true &&
								template.Find("settlement").IsNullOrWhitespace() &&
								template.Find("house").IsNullOrWhitespace() &&
								template.Find("ship").IsNullOrWhitespace() &&
								template.Find("store").IsNullOrWhitespace() &&
								template.Find("loc") is IParameterNode loc &&
								(loc.IsNullOrWhitespace() || string.Equals(loc.Value.ToValue(), "{{huh}}", StringComparison.OrdinalIgnoreCase)))
							{
								retval.Add(npc);
							}
						}
						else if (template is null)
						{
							if (page.IsMissing)
							{
								retval.Add(npc);
							}
							else
							{
								issue = "is already a content page without an Online NPC Summary";
							}
						}
					}

					if (issue != null)
					{
						issues.Add((npc, issue));
					}
				}
			}

			issues.Sort(NpcComparer);
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Page !! Issue !! NPC Data");
			foreach (var (npc, issue) in issues)
			{
				this.WriteLine("|-");
				this.WriteLine($"| {npc.Page?.AsLink(LinkFormat.LabelName)}");
				this.WriteLine($"| {issue}");
				this.WriteLine("| " + NpcToWikiText(npc));
			}

			this.WriteLine("|}");
			return retval;
		}
		#endregion
	}
}
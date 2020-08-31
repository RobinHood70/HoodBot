namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby.Parser;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon; using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon; using RobinHood70.CommonCode;
	using static RobinHood70.CommonCode.Globals;

	public class DBMergeArticles : PageMoverJob
	{
		#region Static Fields
		private static readonly Dictionary<string, bool?> TrailTemplates = new Dictionary<string, bool?>()
		{
			["City Summary"] = false,
			["Creature Summary"] = false,
			["Disambig"] = null,
			["disambig"] = null,
			["Effect Summary"] = false,
			["Game Book"] = false,
			["Game Book Compilation"] = false,
			["Ingredient Summary"] = false,
			["NPC Summary"] = false,
			["NPC Summary Multi"] = false,
			["Place Summary"] = false,
			["Spell Summary"] = false,
			["Trail"] = true,
			["Trail2"] = true,
		};
		#endregion

		#region Fields
		private readonly Dictionary<string, List<string>> dbArchives = new Dictionary<string, List<string>>();
		#endregion

		#region Constructors
		[JobInfo("Merge Pages", "Dragonborn Merge")]
		public DBMergeArticles(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.CustomEdit = this.UpdatePage;
			this.MoveAction = MoveAction.None;
			this.FollowUpActions = FollowUpActions.CheckLinksRemaining | FollowUpActions.FixLinks;
			this.RedirectOption = RedirectOption.Create;
			this.ReplaceSingleNode = this.FixNsBase;
			this.TemplateReplacements.Add("About", this.AboutHandler);
			this.TemplateReplacements.Add("Discussion Moved", this.DiscussionMovedHandler);
			this.TemplateReplacements.Add("Dm", this.DiscussionMovedHandler);
			this.TemplateReplacements.Add("Lore Alchemy Entry", this.LoreAlchemyHandler);
			this.TemplateReplacements.Add("Lore Book", this.LoreBookHandler);
			this.TemplateReplacements.Add("Lore Book Compilation", this.LoreBookHandler);
			this.TemplateReplacements.Add("Sandbox Space", this.SandboxSpaceHandler);
			this.TemplateReplacements.Add("SR Spell", this.SRSpellHandler);
		}
		#endregion

		#region Public Static Properties
		public static Dictionary<string, string> SpecialCases => new Dictionary<string, string>
		{
			["Benkum"] = "Benkum (Dragonborn)",
			["Eydis"] = "Eydis (Dragonborn)",
			["Items"] = "Dragonborn Items",
			["Letter"] = "Letter (Dragonborn)",
			["Liesl"] = "Liesl (Dragonborn)",
			["Mogrul"] = "Mogrul (Dragonborn)",
			["Nikulas"] = "Nikulas (Dragonborn)",
			["NPCs"] = "Dragonborn NPCs",
			["Places"] = "Dragonborn Places",
			["Quests"] = "Dragonborn Quests",
			["Torn Note"] = "Torn Note (Dragonborn)",
		};
		#endregion

		#region Public Static Methods
		public static void UpdateFileParameter(PageMoverJob job, int ns, NodeCollection value)
		{
			ThrowNull(job, nameof(job));
			ThrowNull(value, nameof(value));
			var split = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(value));
			var title = new Title(job.Site, ns, split.Value);
			if (job.Replacements.TryGetValue(title, out var replacement))
			{
				split.Value = replacement.To.PageName;
				value.Clear();
				value.AddText(split.ToString());
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void EditAfterMove()
		{
			foreach (var replacement in this.Replacements)
			{
				if (!replacement.Actions.HasFlag(ReplacementActions.Skip))
				{
					this.EditDictionary.Add(replacement.From, replacement);
					if (replacement.To.PageName.Contains("/Dragonborn Archive", StringComparison.Ordinal))
					{
						var basePageName = replacement.To.BasePageName;
						var talkTitle = new TitleParts(this.Site, replacement.To.NamespaceId, basePageName);

						// This is a bit kludgey, and would make more sense outside of the EditDictionary context, but this avoids multiple edits to the same page.
						this.EditDictionary.TryAdd(talkTitle, replacement);
						if (!this.dbArchives.TryGetValue(basePageName, out var list))
						{
							list = new List<string>();
							this.dbArchives[basePageName] = list;
						}

						var archiveName = replacement.To.SubpageName;
						list.Add(archiveName);
					}
				}
			}

			base.EditAfterMove();
		}

		protected override void FilterBacklinks(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			//// base.FilterBacklinks(backlinkTitles);
			backlinkTitles.Remove("User:HoodBot/Dragonborn Merge Actions");
			backlinkTitles.Remove("UESPWiki:Dragonborn Merge Project");

			// Fix Lore Books
			backlinkTitles.GetBacklinks("Template:Lore Book", BacklinksTypes.EmbeddedIn);
			backlinkTitles.GetBacklinks("Template:Lore Book Compilation", BacklinksTypes.EmbeddedIn);
		}

		protected override void PopulateReplacements()
		{
			var dbPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			dbPages.GetNamespace(UespNamespaces.Dragonborn, Filter.Any);
			dbPages.GetNamespace(UespNamespaces.DragonbornTalk, Filter.Any);
			dbPages.Sort();

			this.RemoveProposed(dbPages);
			this.RemoveNoBot(dbPages);
			this.RemoveMergedRedirects(dbPages);
			this.RemoveMerged(dbPages);

			this.AddReplacements(dbPages);
			this.AdjustTalkPages();
		}
		#endregion

		#region Private Static Methods
		private static void AddModHeader(ContextualParser parser)
		{
			if (parser.FindFirst<TemplateNode>(item => item.GetTitleValue() == "Mod Header") == null)
			{
				if (parser.FindFirstLinked<TemplateNode>(item => TrailTemplates.ContainsKey(item.GetTitleValue())) is LinkedListNode<IWikiNode> trail)
				{
					var trailTemplate = (TemplateNode)trail.Value;
					var addAfter = TrailTemplates[trailTemplate.GetTitleValue()];
					var modHeader = TemplateNode.FromText("{{Mod Header|Dragonborn}}");
					if (addAfter == true)
					{
						parser.AddAfter(trail, modHeader);
					}
					else if (addAfter == false)
					{
						parser.AddBefore(trail, modHeader);
					}
				}
				else
				{
					Debug.WriteLine($"{parser.Title?.FullPageName} has no Trail template.");
				}
			}
		}

		private static void RemoveTitleandTalk(PageCollection dbPages, Title title)
		{
			dbPages.Remove(title.SubjectPage);
			if (title.TalkPage != null)
			{
				dbPages.Remove(title.TalkPage);
			}
		}

		private static void UpdateArchiveTable(ContextualParser parser, List<string> archiveList)
		{
			var template = parser.FindFirst<TemplateNode>(item => item.GetTitleValue().ToLowerInvariant() == "archive table");
			if (template == null)
			{
				template = TemplateNode.FromParts("Archive Table\n");
				var lead = parser.First?.Value;
				if (lead is HeaderNode)
				{
					parser.AddFirst(new TextNode("\n\n"));
				}
				else if (lead is TextNode textNode)
				{
					textNode.Text = "\n\n" + textNode.Text.TrimStart();
				}

				parser.AddFirst(template);
			}

			// Imperfect, but simple and probably always going to be right.
			var firstAnon = template.FindNumberedParameterLinked(1);
			archiveList.Sort();
			foreach (var archive in archiveList)
			{
				var text = ParameterNode.FromParts(archive);
				var node = firstAnon == null ? template.Parameters.AddLast(text) : template.Parameters.AddBefore(firstAnon, text);
				node = template.Parameters.AddAfter(node, ParameterNode.FromParts(string.Empty));
				node = template.Parameters.AddAfter(node, ParameterNode.FromParts("\n"));
			}
		}
		#endregion

		#region Private Methods
		private void AboutHandler(Page page, TemplateNode template)
		{
			foreach (var parameter in template.NumberedParameters)
			{
				var index = parameter.Index;
				if (index > 1 && (index & 1) == 1)
				{
					this.UpdateLinkParameter(page, parameter.Parameter, true);
				}
			}
		}

		private void UpdateLinkParameter(Page page, ParameterNode param, bool trimMatchingNs)
		{
			var split = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(param.Value));
			var title = new TitleParts(this.Site, page.NamespaceId, split.Value);
			if (this.Replacements.TryGetValue(title, out var replacement) && replacement.Actions.HasFlag(ReplacementActions.Move))
			{
				title.NamespaceId = trimMatchingNs ? MediaWikiNamespaces.Main : replacement.To.NamespaceId;
				title.PageName = replacement.To.PageName;
				split.Value = title.ToString();
				param.SetValue(split.ToString());
			}
		}

		private void AddReplacements(PageCollection dbPages)
		{
			foreach (var dbPage in dbPages)
			{
				if (!SpecialCases.TryGetValue(dbPage.PageName, out var srPageName))
				{
					srPageName = dbPage.PageName;
				}

				var srTitle = new Title(this.Site, dbPage.Namespace.IsTalkSpace ? UespNamespaces.SkyrimTalk : UespNamespaces.Skyrim, srPageName);
				if (this.ProposedDeletions.Contains(srTitle))
				{
					this.Warn($"* {srTitle.AsLink()} is proposed for deletion. Admin may want to delete before bot run.");
				}

				var replacement = new Replacement(dbPage, srTitle);
				replacement.Actions |= ReplacementActions.Edit;
				this.Replacements.Add(replacement);
			}
		}

		private void AdjustTalkPages()
		{
			TitleParts BaseSubjectPage(ISimpleTitle to) => new TitleParts(this.Site, to.SubjectPage.NamespaceId, to.BasePageName);

			// This turned into a great big mess, but it works, so I'm not touching it again!
			var srTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				if (replacement.To.Namespace.IsTalkSpace)
				{
					srTitles.Add(BaseSubjectPage(replacement.To));
				}
			}

			var srPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			srPages.GetTitles(srTitles);
			foreach (var replacement in this.Replacements)
			{
				if (replacement.To.Namespace.IsTalkSpace)
				{
					var baseTitle = BaseSubjectPage(replacement.To);
					if (srPages.TryGetValue(baseTitle, out var srBasePage) && srBasePage.Exists)
					{
						if (srBasePage.IsRedirect || srBasePage.IsDisambiguation)
						{
							replacement.Actions = ReplacementActions.Skip;
							replacement.Reason = $"{baseTitle.AsLink()} is a redirect or disambig";
						}
						else
						{
							var title = new TitleParts(replacement.To);
							title.PageName =
								title.PageName == "Easter Eggs" ? "Easter Eggs/Dragonborn Archive 3" :
								!title.PageName.Contains('/', StringComparison.Ordinal) ? title.PageName + "/Dragonborn Archive" :
								title.PageName.Contains("/Archive", StringComparison.Ordinal) ? title.PageName.Replace("/Archive", "/Dragonborn Archive", StringComparison.Ordinal) :
								title.BasePageName + "/Dragonborn Archive";
							replacement.To = title;
						}
					}
				}
			}
		}

		private void DiscussionMovedHandler(Page page, TemplateNode template)
		{
			foreach (var param in template.NumberedParameters)
			{
				this.UpdateLinkParameter(page, param.Parameter, false);
			}
		}

		private void FixNsBase(Page page, IWikiNode node)
		{
			if (node is TemplateNode template && template.GetTitleValue()?.ToLowerInvariant() != "map link")
			{
				foreach (var param in template.FindParameters("ns_base", "ns_id"))
				{
					var value = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(param.Value));
					if (this.Site.Namespaces[UespNamespaces.Dragonborn].Contains(value.Value))
					{
						if (page.Namespace.SubjectSpaceId == UespNamespaces.Skyrim)
						{
							template.Parameters.Remove(param);
						}
						else
						{
							param.SetValue(value.Value == "DB" ? "SR" : "Skyrim");
						}
					}
				}
			}

			/*
			if (node is IBacklinkNode backlinkNode)
			{
				var titleText = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(backlinkNode.Title));
				var title = new TitleParts(this.Site, titleText.Value);
				if (title.NamespaceId == UespNamespaces.Dragonborn || title.NamespaceId == UespNamespaces.DragonbornTalk)
				{
					title.NamespaceId = title.Namespace.IsTalkSpace ? UespNamespaces.SkyrimTalk : UespNamespaces.Skyrim;
					titleText.Value = title.ToString();
					backlinkNode.Title.Clear();
					backlinkNode.Title.AddText(titleText.Value);
				}
			}
			*/
		}

		private void LoreAlchemyHandler(Page page, TemplateNode template)
		{
			var totalParameters = new List<(int, ParameterNode)>(template.NumberedParameters);
			for (var index = totalParameters.Count; index % 4 != 0; index++)
			{
				template.AddParameter(string.Empty);
			}

			var addText = false;
			var ingName = template.ValueOf("name")?.Trim();
			foreach (var parameter in template.NumberedParameters)
			{
				var relIndex = parameter.Index % 4;
				if (relIndex == 1)
				{
					var value = WikiTextVisitor.Value(parameter.Parameter.Value).Trim();
					if (this.Site.Namespaces[UespNamespaces.Dragonborn].Contains(value))
					{
						parameter.Parameter.SetValue("SR");
						addText = true;
					}
				}

				if (addText && relIndex == 2)
				{
					var value = WikiTextVisitor.Value(parameter.Parameter.Value).Trim();
					if (value.Length > 0)
					{
						ingName = value;
					}
				}

				if (relIndex == 0)
				{
					if (addText)
					{
						var value = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(parameter.Parameter.Value));
						if (value.Value.Length == 0)
						{
							value.Value = $"[[Skyrim:{ingName}|Skyrim]] ([[Skyrim:Dragonborn|Dragonborn]])";
							parameter.Parameter.SetValue(value.ToString());
						}
						else if (value.Value.Contains(ingName + "|Dragonborn]]", StringComparison.OrdinalIgnoreCase))
						{
							value.Value = value.Value.Replace(ingName + "|Dragonborn]]", ingName + "|Skyrim]] ([[Skyrim:Dragonborn|Dragonborn]])", StringComparison.OrdinalIgnoreCase);
							parameter.Parameter.SetValue(value.ToString());
						}
						else
						{
							Debug.WriteLine($"{page.AsLink()}/{ingName} has a non-blank name parameter for Skyrim, but does not include Dragonborn in it.");
						}
					}

					addText = false;
				}
			}
		}

		private void LoreBookHandler(Page page, TemplateNode template)
		{
			LinkedListNode<IWikiNode>? addExtra = null;
			foreach (var node in template.Parameters.LinkedNodes)
			{
				var parameter = (ParameterNode)node.Value;
				var name = parameter.NameToText();
				if ((name == "DB" || name == "DBName") && parameter.Value.Count > 0)
				{
					parameter.SetName(name == "DB" ? "SR" : "SRName");
					addExtra = node;
				}

				if (name == "DBExtra")
				{
					parameter.SetName("SRExtra");
				}
			}

			if (addExtra != null)
			{
				template.Parameters.AddAfter(addExtra, ParameterNode.FromParts("SRExtra", "([[Skyrim:Dragonborn|Dragonborn]])\n"));
			}
		}

		private void RemoveProposed(PageCollection dbPages)
		{
			foreach (var title in this.ProposedDeletions)
			{
				RemoveTitleandTalk(dbPages, title);
			}
		}

		private void RemoveNoBot(PageCollection dbPages)
		{
			var ignored = new TitleCollection(this.Site);
			ignored.GetCategoryMembers("DBMerge-No Bot");
			foreach (var title in ignored)
			{
				for (var i = dbPages.Count - 1; i >= 0; i--)
				{
					var dbPage = dbPages[i];
					if (dbPage.BasePageName == title.BasePageName)
					{
						dbPages.RemoveAt(i);
					}
				}
			}
		}

		private void RemoveMerged(PageCollection dbPages)
		{
			var ignored = new TitleCollection(this.Site);
			ignored.GetCategoryMembers("DBMerge-Merged");
			dbPages.Remove("Dragonborn:Miscellaneous Quests"); // Manual removal so we move subpages but not main page
			dbPages.Remove("Dragonborn talk:Miscellaneous Quests");
			ignored.Remove("Skyrim:Miscellaneous Quests");
			foreach (var title in ignored)
			{
				for (var i = dbPages.Count - 1; i >= 0; i--)
				{
					var dbPage = dbPages[i];
					if (dbPage.BasePageName == title.BasePageName && !dbPage.Namespace.IsTalkSpace)
					{
						dbPages.RemoveAt(i);
					}
				}
			}
		}

		private void RemoveMergedRedirects(PageCollection dbPages)
		{
			var ignored = new TitleCollection(this.Site);
			ignored.GetCategoryMembers("DBMerge-Redirects");
			ignored.RemoveNamespaces(UespNamespaces.Skyrim); // Only relevant to dev because of bot error.
			ignored.Remove("Dragonborn:Places"); // Also dev-only. Transcludes other pages with the redirect. Should not be present in real run, but safe either way.
			ignored.Remove("Dragonborn talk:Places");
			foreach (var title in ignored)
			{
				dbPages.Remove(title);
			}
		}

		private void SandboxSpaceHandler(Page page, TemplateNode template)
		{
			if (template.FindNumberedParameter(1) is ParameterNode nsBase)
			{
				var value = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(nsBase.Value));
				if (this.Site.Namespaces[UespNamespaces.Dragonborn].Contains(value.Value))
				{
					nsBase.SetValue(value.Value == "DB" ? "SR" : "Skyrim");
				}
			}
		}

		private void SRSpellHandler(Page page, TemplateNode template) => template.RemoveParameter("iconns");

		private void UpdatePage(Page page, Replacement replacement)
		{
			if (!replacement.Actions.HasFlag(ReplacementActions.Move))
			{
				Debug.WriteLine(page.FullPageName + " was not a move");
				return;
			}

			if (page.IsRedirect)
			{
				if (page.SubjectPage.NamespaceId == UespNamespaces.Dragonborn && !page.Text.Contains("[[Category:", StringComparison.Ordinal))
				{
					page.Text += "\n\n[[Category:Redirects from Moves]]";
				}
			}
			else
			{
				var parser = ContextualParser.FromPage(page);
				if (!page.Namespace.IsTalkSpace)
				{
					AddModHeader(parser);
				}
				else if (this.dbArchives.TryGetValue(page.BasePageName, out var archiveList))
				{
					UpdateArchiveTable(parser, archiveList);
				}

				this.ReplaceNodes(page, parser);
				page.Text = WikiTextVisitor.Raw(parser);
			}
		}
		#endregion
	}
}

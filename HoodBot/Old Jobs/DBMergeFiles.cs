namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class DBMergeFiles : PageMoverJob
	{
		#region Constructors
		[JobInfo("Merge Files", "Dragonborn Merge")]
		public DBMergeFiles(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			// this.FollowUpActions = FollowUpActions.EmitReport;
			this.FollowUpActions = FollowUpActions.EmitReport | FollowUpActions.ProposeUnused | FollowUpActions.FixLinks | FollowUpActions.CheckLinksRemaining;
			this.TemplateReplacements.Add("Gameinfo", this.GameInfoHandler);
			this.TemplateReplacements.Add("Icon", this.IconHandler);
			this.TemplateReplacements.Add("Multiple images", this.MultipleImagesHandler);
			this.TemplateReplacements.Add("Multiple Images 2", this.MultipleImages2Handler);
			this.TemplateReplacements.Add("Artifact Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("City Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Creature Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Ingredient Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Item Data", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Lore Entry", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Lore People Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Lore Place Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("NPC Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("NPC Summary Multi", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Place Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Quest Fork", this.QuestForkHandler);
			this.TemplateReplacements.Add("Quest Header", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Shout Summary", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Skyrim Settlements", this.DroppedNsHandler);
			this.TemplateReplacements.Add("Spell Summary", this.DroppedNsHandler);
			//// this.ReplaceSingleNode = this.FixNsBase;
		}
		#endregion

		#region Protected Override Methods
		protected override void FilterBacklinks(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			base.FilterBacklinks(backlinkTitles);
			backlinkTitles.Remove("User:HoodBot/Dragonborn Merge Actions");
			backlinkTitles.Remove("UESPWiki:Dragonborn Merge Project");
		}

		protected override void PopulateReplacements()
		{
			var deleted = TitleCollection.CopyFrom(this.ProposedDeletions);
			deleted.FilterToNamespaces(MediaWikiNamespaces.File);

			var dbFiles = new TitleCollection(this.Site);
			dbFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "DB-");
			dbFiles.GetNamespace(MediaWikiNamespaces.FileTalk, Filter.Exclude, "DB-");
			foreach (var page in deleted)
			{
				dbFiles.Remove(page);
			}

			var srFiles = new TitleCollection(this.Site);
			srFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "SR-");
			srFiles.GetNamespace(MediaWikiNamespaces.FileTalk, Filter.Exclude, "SR-");

			foreach (var dbFile in dbFiles)
			{
				var fileName = "SR-" + dbFile.PageName.Substring(3).Replace(" 01.", ".", StringComparison.Ordinal);
				var extension = fileName.Substring(fileName.LastIndexOf('.'));
				fileName = fileName.Substring(0, fileName.Length - extension.Length);
				fileName = fileName switch
				{
					"SR-creature-Ash Spawn2" => "SR-creature-Ash Spawn 3",
					"SR-creature-Ash Spawn 02" => "SR-creature-Ash Spawn 4",
					"SR-book-Fahlbtharzjournal02" => "SR-book-Fahlbtharzjournal 02",
					"SR-book-Fahlbtharzjournal03" => "SR-book-Fahlbtharzjournal 03",
					_ => fileName
				};

				if (srFiles.Contains(dbFile.Namespace.DecoratedName + fileName + extension))
				{
					int num;
					var found = true;
					var numLength = 1;
					for (num = 2; found; num++)
					{
						found = srFiles.Contains($"{dbFile.Namespace.DecoratedName}{fileName} {num}{extension}");
						if (!found)
						{
							found = srFiles.Contains($"{dbFile.Namespace.DecoratedName}{fileName} 0{num}{extension}");
							if (found && numLength == 1)
							{
								// Stupidly primitive, but it'll do for this.
								numLength = 2;
							}
						}
					}

					num--;
					var numText = "0" + num.ToString();
					numText = numText.Substring(numText.Length - numLength);
					fileName = $"{fileName} {numText}";
				}

				var srFile = new Title(this.Site, dbFile.Namespace.DecoratedName + fileName + extension);
				this.Replacements.Add(new Replacement(dbFile, srFile));
			}
		}
		#endregion

		#region Private Methods
		private void DroppedNsHandler(Page page, TemplateNode template)
		{
			foreach (var parameter in template.FindParameters(true, "icon", "image"))
			{
				DBMergeArticles.UpdateFileParameter(this, MediaWikiNamespaces.File, parameter.Value);
			}
		}

		private void IconHandler(Page page, TemplateNode template)
		{
			if (page.Namespace.SubjectSpaceId != UespNamespaces.Dragonborn && template.FindParameterLinked("ns_base") is LinkedListNode<IWikiNode> paramListNode && paramListNode.Value is ParameterNode nsBase)
			{
				var value = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(nsBase.Value));
				if (this.Site.Namespaces[UespNamespaces.Dragonborn].Contains(value.Value))
				{
					if (page.Namespace.SubjectSpaceId == UespNamespaces.Skyrim)
					{
						template.Parameters.Remove(nsBase);
					}
					else
					{
						nsBase.SetValue(value.Value == "DB" ? "SR" : "Skyrim");
					}
				}
			}
		}

		private void GameInfoHandler(Page page, TemplateNode template)
		{
			foreach (ParameterNode parameter in template.Parameters)
			{
				if (parameter.NameToText() is string name && name.EndsWith("image", StringComparison.Ordinal))
				{
					DBMergeArticles.UpdateFileParameter(this, MediaWikiNamespaces.File, parameter.Value);
				}
			}
		}

		private void MultipleImagesHandler(Page page, TemplateNode template)
		{
			foreach (ParameterNode parameter in template.Parameters)
			{
				if (parameter.NameToText() is string name && name.StartsWith("image", StringComparison.Ordinal))
				{
					DBMergeArticles.UpdateFileParameter(this, MediaWikiNamespaces.File, parameter.Value);
				}
			}
		}

		private void MultipleImages2Handler(Page page, TemplateNode template)
		{
			foreach (var parameter in template.NumberedParameters)
			{
				var index = parameter.Index;
				if ((index & 1) == 1)
				{
					DBMergeArticles.UpdateFileParameter(this, MediaWikiNamespaces.File, parameter.Parameter.Value);
				}
			}
		}

		private void QuestForkHandler(Page page, TemplateNode template)
		{
			foreach (var parameter in template.FindParameters(true, "imageL", "imageL2", "imageR", "imageR2"))
			{
				DBMergeArticles.UpdateFileParameter(this, MediaWikiNamespaces.File, parameter.Value);
			}
		}
		#endregion
	}
}
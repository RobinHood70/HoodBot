namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class MovePages : MovePagesJob
	{
		#region Static Fields
		private static readonly List<string> PhotoCats =
		new()
		{
			"Morrowind-Photos-rpeh",
			"Morrowind-Photos-TheEnigmaticMan",
			"Oblivion-Photos-Arch-Mage Matt",
			"Oblivion-Photos-rpeh",
			"Oblivion-Photos-TheEnigmaticMan",
			"Oblivion-Photos-Timenn",
			"Skyrim-Photos-rpeh"
		};
		#endregion

		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(JobManager jobManager)
				: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.MoveSafely;
			this.MoveDelay = 0;
			this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.EmitReport | FollowUpActions.CheckLinksRemaining;
		}
		#endregion

		#region Protected Override Methods
		protected override void CustomEdit(Page page, Replacement replacement)
		{
			var parsedPage = new ContextualParser(page);
			var nodes = parsedPage.Nodes;
			for (var i = nodes.Count - 1; i >= 0; i--)
			{
				if (nodes[i] is SiteLinkNode link &&
					link.TitleValue.Namespace == MediaWikiNamespaces.Category &&
					PhotoCats.Contains(link.TitleValue.PageName, StringComparer.OrdinalIgnoreCase))
				{
					nodes.RemoveAt(i);
					if (i < (nodes.Count - 1) && nodes[i + 1] is ITextNode text)
					{
						text.Text = text.Text.TrimStart();
					}
				}
			}

			page.Text = parsedPage.ToRaw();
		}

		protected override void PopulateReplacements()
		{
			foreach (var cat in PhotoCats)
			{
				this.GetAuthorReplacements(cat);
			}

			this.AddReplacement("Category:Photographs", "Category:User Images-Screenshots");
			this.AddReplacement("Category:Morrowind-Photos", "Category:User Images-Screenshots-Morrowind");
			this.AddReplacement("Category:Oblivion-Photos", "Category:User Images-Screenshots-Oblivion");
			this.AddReplacement("Category:Skyrim-Photos", "Category:User Images-Screenshots-Skyrim");
		}
		#endregion

		#region Private Methods
		private void GetAuthorReplacements(string authorCat)
		{
			var author = authorCat.Split(TextArrays.CategorySeparators, 3)[^1];
			var titles = new TitleCollection(this.Site);
			titles.GetCategoryMembers(authorCat);
			foreach (var title in titles)
			{
				var pageParts = title.PageName.Split(TextArrays.CategorySeparators, 3);
				var imageName = pageParts[^1];
				var rep = new Replacement(
					title,
					Title.FromName(this.Site, MediaWikiNamespaces.File, $"User-{author}-{imageName}"));
				rep.Actions |= ReplacementActions.Edit;
				this.Replacements.Add(rep);
			}
		}
		#endregion
	}
}
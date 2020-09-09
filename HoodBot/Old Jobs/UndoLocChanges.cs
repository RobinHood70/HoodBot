namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon; using RobinHood70.CommonCode;

	public class UndoLocChanges : EditJob
	{
		#region Fields
		private readonly TitleCollection editedPages;
		#endregion

		#region Constructors
		[JobInfo("Undo Location Changes")]
		public UndoLocChanges([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.LogDetails = "Removes all locations added during Sept. 19 bot run while leaving any other location changes intact.";
			this.editedPages = new TitleCollection(site);
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Undo Location Changes";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.StatusWriteLine("Saving pages");
			this.Pages.RemoveUnchanged();
			this.Progress = 0;
			this.ProgressMaximum = this.Pages.Count;
			this.Pages.Sort();
			this.EditConflictAction = (job, page) => this.WriteLine($"* [[{page.FullPageName}]] had an edit conflict and was skipped. Re-run bot job after this run is complete.");
			foreach (var page in this.Pages)
			{
				this.SavePage(page, "Revert Sept. 19 location changes", true);
				this.Progress++;
			}
		}

		protected override void PrepareJob()
		{
			var revIds = this.CreateRevIdsList();
			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.Pages.GetRevisionIds(revIds);
			this.Pages.PageLoaded -= this.Pages_PageLoaded;

			if (this.editedPages.Count > 0)
			{
				this.editedPages.Sort();
				this.WriteLine($"The following pages have had their <code>loc</code> parameter modified since the bot ran. The location has been updated on a best-effort basis, but may need human intervention.");
				foreach (var title in this.editedPages)
				{
					this.WriteLine($"* [[{title.FullPageName}]]");
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static LinkedListNode<IWikiNode>? GetLocation(NodeCollection parser)
		{
			var templates = parser.FindAll<TemplateNode>(template => WikiTextVisitor.Value(template.Title).Trim() == "Online NPC Summary");
			var list = new List<TemplateNode>(templates);
			return list.Count == 1 ? list[0].Parameters.FindLastLinked<ParameterNode>(param => param.Name != null && WikiTextVisitor.Value(param.Name).Trim() == "loc") : null;
		}

		private static List<string> LocSplit(NodeCollection valueNodes)
		{
			var retval = new List<string>(valueNodes.Count << 1)
			{
				string.Empty
			};

			foreach (var node in valueNodes)
			{
				if (node is TextNode text)
				{
					var startAt = 0;
					var split = text.Text.Split(TextArrays.CommaSpace, StringSplitOptions.None);
					if (split[0].Length > 0)
					{
						retval[retval.Count - 1] += split[0];
						startAt++;
					}

					for (var i = startAt; i < split.Length; i++)
					{
						retval.Add(split[i]);
					}
				}
				else
				{
					retval[retval.Count - 1] += WikiTextVisitor.Raw(node);
				}
			}

			for (var i = retval.Count - 1; i >= 0; i--)
			{
				retval[i] = retval[i].Trim();
				if (retval[i].Length == 0)
				{
					retval.RemoveAt(i);
				}
			}

			return retval;
		}
		#endregion

		#region Private Methods
		private SortedSet<long> CreateRevIdsList()
		{
			var revIds = new SortedSet<long>();
			var from = new DateTime(2019, 9, 19, 1, 0, 23, DateTimeKind.Utc);
			var to = new DateTime(2019, 9, 19, 11, 58, 41, DateTimeKind.Utc);
			var user = this.Site.User;
			if (user == null)
			{
				throw new InvalidOperationException();
			}

			var contributions = user.GetContributions(from, to);
			var titles = new TitleCollection(this.Site);
			foreach (var contribution in contributions)
			{
				revIds.Add(contribution.Id);
				if (contribution.ParentId != 0)
				{
					revIds.Add(contribution.ParentId);
				}

				titles.Add(contribution.Title);
			}

			var current = new PageCollection(this.Site, PageModules.Info);
			current.GetTitles(titles);
			foreach (var page in current)
			{
				if (page.CurrentRevisionId != 0)
				{
					revIds.Add(page.CurrentRevisionId);
				}
			}

			return revIds;
		}

		private void Pages_PageLoaded(object sender, Page page)
		{
			if (page.Revisions.Count > 1)
			{
				var parser = NodeCollection.Parse(page.CurrentRevision!.Text);
				var newer = GetLocation(NodeCollection.Parse(page.Revisions[1].Text));
				var older = GetLocation(NodeCollection.Parse(page.Revisions[0].Text));
				var current = GetLocation(parser);
				if (current == null || newer == null)
				{
					return;
				}

				var currentNode = current.Value as ParameterNode;
				var newerNode = newer.Value as ParameterNode;
				var olderNode = older?.Value as ParameterNode;

				var newSplit = LocSplit(newerNode.Value);
				var currentSplit = LocSplit(currentNode.Value);
				if (olderNode != null)
				{
					var oldSplit = LocSplit(olderNode.Value);
					foreach (var value in oldSplit)
					{
						newSplit.Remove(value);
					}
				}

				var removeCount = 0;
				foreach (var value in newSplit)
				{
					if (currentSplit.Remove(value))
					{
						removeCount++;
					}
				}

				if (older == null && currentSplit.Count == 0)
				{
					// Remove current node if old node didn't exist, list was completely removed, and current values are empty.
					current.List.Remove(current);
				}
				else
				{
					var currentText = string.Join(", ", currentSplit) + '\n';
					currentNode.Value.Clear();
					currentNode.Value.AddRange(NodeCollection.Parse(currentText));

					var olderText = olderNode == null ? "\n" : WikiTextVisitor.Raw(olderNode.Value);
					if (removeCount < newSplit.Count && currentText != olderText)
					{
						this.editedPages.Add(page);
					}
				}

				page.Text = WikiTextVisitor.Raw(parser);
			}
		}
		#endregion
	}
}
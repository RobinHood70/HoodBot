﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;

	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	// TODO: Rewrite this class when more clear-headed...this is beyond fugly!
	public class FixDoubleRedirects : EditJob
	{
		#region Fields
		private readonly Dictionary<Title, FullTitle> lookup = new();
		private readonly Dictionary<Title, ContextualParser> parsedPages = new();
		private readonly IReadOnlyCollection<string> redirectWords;
		#endregion

		#region Constructors
		[JobInfo("Fix Double Redirects", "Maintenance|")]
		public FixDoubleRedirects(JobManager jobManager)
			: base(jobManager)
		{
			this.redirectWords = this.Site.MagicWords["redirect"].Aliases;
		}
		#endregion

		#region Protected Override Properties
		protected override Action<EditJob, Page>? EditConflictAction => null;

		protected override string EditSummary => "Fix double redirect";
		#endregion

		#region Protected Override Methods
		protected override void AfterLoadPages()
		{
			HashSet<FullTitle> loopCheck = new();
			HashSet<string> fragments = new(StringComparer.Ordinal);
			foreach (var page in this.Pages)
			{
				if (this.lookup.TryGetValue(page, out var originalTarget))
				{
					loopCheck.Clear();
					fragments.Clear();
					var loop = false;
					var target = originalTarget;
					if (originalTarget.Fragment != null)
					{
						fragments.Add(originalTarget.Fragment);
					}

					loopCheck.Add(target);
					//// Debug.Write(target.ToString());
					while (this.lookup.TryGetValue(target, out var newTarget))
					{
						if (loopCheck.Contains(newTarget))
						{
							Debug.WriteLine("Loop detected!");
							loop = true;
							break;
						}

						if (newTarget.Fragment != null)
						{
							fragments.Add(newTarget.Fragment);
						}

						target = newTarget;
						//// Debug.Write(" --> " + target.ToString());
					}

					Debug.WriteLine(string.Empty);

					if (loop)
					{
						continue;
					}

					FullTitle comboTarget = new((IFullTitle)target);
					if (fragments.Count == 1)
					{
						comboTarget.Fragment = fragments.First();
					}
					else if (fragments.Count > 1)
					{
						Debug.WriteLine("Fragment conflict on " + page.FullPageName);
						continue;
					}

					if (this.parsedPages.TryGetValue(page, out var parser) && parser.Find<ILinkNode>() is ILinkNode linkNode)
					{
						// linkNode.Parameters.Clear();
						if (!comboTarget.FullEquals(originalTarget) && comboTarget.ToString() is string newValue)
						{
							linkNode.Parameters.Clear(); // For now, only do this if we're updating anyway.
							linkNode.Title.Clear();
							linkNode.Title.AddText(newValue);
						}

						parser.UpdatePage();
					}
				}
			}
		}

		protected override void LoadPages()
		{
			this.Pages.GetQueryPage("DoubleRedirects");
			var toLoad = this.GetNewTitles(this.Pages);
			while (toLoad.Count > 0)
			{
				PageCollection tempPages = new(this.Site);
				tempPages.GetTitles(toLoad);
				foreach (var page in tempPages)
				{
					if (page.IsRedirect)
					{
						this.Pages.Add(page);
					}
				}

				toLoad = this.GetNewTitles(toLoad);
			}
		}
		#endregion

		#region Private Methods
		private TitleCollection GetNewTitles(IReadOnlyCollection<Title> toLoad)
		{
			TitleCollection retval = new(this.Site);
			foreach (var title in toLoad)
			{
				if (this.Pages.TryGetValue(title, out var page))
				{
					ContextualParser parser = new(page);
					if (parser.Count > 0 && parser[0] is ITextNode textNode && this.redirectWords.Contains(textNode.Text.TrimEnd(), StringComparer.OrdinalIgnoreCase))
					{
						if (parser.Find<ILinkNode>() is ILinkNode targetNode)
						{
							FullTitle target = TitleFactory.FromBacklinkNode(this.Site, targetNode);
							if (this.lookup.TryAdd(title, target))
							{
								this.parsedPages.Add(title, parser);
								retval.Add(target);
							}
						}
						else
						{
							throw new InvalidOperationException();
						}
					}
				}
			}

			retval.Sort();
			return retval;
		}
		#endregion
	}
}
namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	// TODO: Rewrite this class when more clear-headed...this is beyond fugly!
	public class FixDoubleRedirects : EditJob
	{
		#region Fields
		private readonly Dictionary<Title, FullTitle> lookup = new Dictionary<Title, FullTitle>();
		private readonly Dictionary<Title, ContextualParser> parsedPages = new Dictionary<Title, ContextualParser>();
		private readonly IReadOnlyCollection<string> redirectWords;
		#endregion

		#region Constructors
		[JobInfo("Fix Double Redirects", "Maintenance|")]
		public FixDoubleRedirects([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.redirectWords = site.MagicWords["redirect"].Aliases;
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.GetDoubleRedirects();
			var loopCheck = new HashSet<FullTitle>();
			var fragments = new HashSet<string>(StringComparer.Ordinal);
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

					var comboTarget = new FullTitle(target);
					if (fragments.Count == 1)
					{
						comboTarget.Fragment = fragments.First();
					}
					else if (fragments.Count > 1)
					{
						Debug.WriteLine("Fragment conflict on " + page.FullPageName);
						continue;
					}

					if (this.parsedPages.TryGetValue(page, out var parsedPage) && parsedPage.Nodes.Find<ILinkNode>() is ILinkNode linkNode)
					{
						// linkNode.Parameters.Clear();
						if (!comboTarget.FullEquals(originalTarget) && comboTarget.ToString() is string newValue)
						{
							linkNode.Parameters.Clear(); // For now, only do this if we're updating anyway.
							linkNode.Title.Clear();
							linkNode.Title.AddText(newValue);
						}

						page.Text = parsedPage.GetText();
					}
				}
			}
		}

		protected override void Main() => this.SavePages("Fix double redirect", true);
		#endregion

		#region Private Methods
		private void GetDoubleRedirects()
		{
			this.Pages.GetQueryPage("DoubleRedirects");
			//// var testCollection = new TitleCollection(this.Site, "Dragonborn:Armour");
			this.Pages.Sort();
			var toLoad = this.GetNewTitles(this.Pages);
			while (toLoad.Count > 0)
			{
				var tempPages = new PageCollection(this.Site);
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

		private TitleCollection GetNewTitles<TTitle>(TitleCollection<TTitle> toLoad)
			where TTitle : Title
		{
			var retval = new TitleCollection(this.Site);
			foreach (var title in toLoad)
			{
				if (this.Pages.TryGetValue(title.FullPageName, out var page))
				{
					var parser = new ContextualParser(page);
					if (parser.Nodes.Count > 0 && parser.Nodes[0] is ITextNode textNode && this.redirectWords.Contains(textNode.Text.TrimEnd(), StringComparer.OrdinalIgnoreCase))
					{
						if (parser.Nodes.Find<ILinkNode>() is ILinkNode targetNode)
						{
							var target = FullTitle.FromBacklinkNode(this.Site, targetNode);
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
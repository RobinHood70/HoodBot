namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using Ardalis.GuardClauses;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Parse Job")]
		public OneOffParseJob(JobManager jobManager)
			: base(jobManager)
		{
#if DEBUG
			this.Shuffle = true;
#endif
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Convert Mod Header to CC Header";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.Pages.Shuffle();
			base.Main();
		}

		protected override void LoadPages()
		{
			TitleCollection templates = new(this.Site);
			TitleCollection catPages = new(this.Site);
			HashSet<ISimpleTitle> hashset = new(SimpleTitleEqualityComparer.Instance);
			templates.GetBacklinks("Template:Mod Header");
			catPages.GetCategoryMembers("Category:Skyrim-Creation Club", true);
			hashset.UnionWith(templates);
			hashset.IntersectWith(catPages);
			this.Pages.GetTitles(hashset);
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			Guard.Against.Null(parsedPage, nameof(parsedPage));
			var nodes = parsedPage.Nodes;
			var count = 0;
			for (var nodeIndex = nodes.Count - 1; nodeIndex >= 0; nodeIndex--)
			{
				if (nodes[nodeIndex] is SiteTemplateNode template &&
					template.TitleValue.PageNameEquals("Mod Header") &&
					template.FindNumberedIndex(1) is var paramIndex &&
					paramIndex >= 0)
				{
					template.Title.Clear();
					template.Title.AddText("CC Header");
					template.Remove("ns_base");
					if (string.Equals(template.Parameters[paramIndex].Value.ToRaw(), "Creation Club", System.StringComparison.Ordinal))
					{
						if (count == 0)
						{
							template.Parameters.RemoveAt(paramIndex);
						}
						else
						{
							nodes.RemoveAt(nodeIndex);
						}
					}

					count++;
				}
			}
		}
		#endregion
	}
}
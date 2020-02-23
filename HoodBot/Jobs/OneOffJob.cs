namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static WikiCommon.Globals;

	public class OneOffJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Add mod parameter";

		protected override void LoadPages()
		{
			var srGameBooks = new TitleCollection(this.Site);
			srGameBooks.GetBacklinks("Template:Game Book", BacklinksTypes.EmbeddedIn, false, Filter.Exclude, UespNamespaces.Skyrim);
			srGameBooks.GetBacklinks("Template:Game Book Compilation", BacklinksTypes.EmbeddedIn, false, Filter.Exclude, UespNamespaces.Skyrim);

			var mhTitles = new TitleCollection(this.Site);
			mhTitles.GetBacklinks("Template:Mod Header", BacklinksTypes.EmbeddedIn, false, Filter.Exclude, UespNamespaces.Skyrim);

			var combined = new HashSet<Title>(mhTitles, SimpleTitleEqualityComparer.Instance);
			combined.IntersectWith(srGameBooks);
			this.Pages.GetTitles(combined);
		}
		#endregion

		#region Protected Override Methods
		protected override void ParseText(object sender, Page page, ContextualParser parsedPage)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(parsedPage, nameof(parsedPage));
			if ((parsedPage.FindFirst<TemplateNode>(template => template.GetTitleValue() == "Mod Header") is TemplateNode modHeader) && (modHeader.FindNumberedParameter(1)?.ValueToText() == "Dragonborn"))
			{
				var gameBookTemplate = parsedPage.FindFirst<TemplateNode>(template =>
				{
					var name = template.GetTitleValue();
					return name.StartsWith("Game Book", StringComparison.OrdinalIgnoreCase);
				});
				if (gameBookTemplate != null)
				{
					gameBookTemplate.Parameters.AddFirst(ParameterNode.FromParts("smod", "DB\n"));
					gameBookTemplate.Parameters.AddFirst(ParameterNode.FromParts("mod", "[[Skyrim:Dragonborn|Dragonborn]]\n"));
				}
			}
		}
		#endregion
	}
}
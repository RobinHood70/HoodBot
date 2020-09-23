namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class OneOffJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string EditSummary => "Fix issue with Mod Header and Mod Icon Data on same page";

		protected override void LoadPages()
		{
			var iconData = new TitleCollection(this.Site);
			iconData.GetBacklinks("Template:Mod Icon Data", BacklinksTypes.EmbeddedIn);
			var modHeader = new TitleCollection(this.Site);
			modHeader.GetBacklinks("Template:Mod Header", BacklinksTypes.EmbeddedIn);

			var hashSet = new HashSet<ISimpleTitle>(iconData, SimpleTitleEqualityComparer.Instance);
			hashSet.IntersectWith(modHeader);

			this.Pages.GetTitles(hashSet);
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			if (parsedPage.FindTemplate("Mod Icon Data") is ITemplateNode iconTemplate &&
				parsedPage.FindTemplate("Mod Header") is ITemplateNode headerTemplate &&
				iconTemplate.Find(1) is IParameterNode icon)
			{
				headerTemplate.Add("icon", icon.ValueToText());
				parsedPage.Nodes.Remove(iconTemplate);
			}
		}
		#endregion
	}
}
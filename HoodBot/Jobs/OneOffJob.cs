namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
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

		#region Public Override Properties
		public override string? LogDetails => "Map Link update for Dragonborn links";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Rename parameter for changes in template";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Map Link", BacklinksTypes.EmbeddedIn, false, Filter.Any);

		protected override void ParseText(object sender, Page page, ContextualParser parsedPage)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(parsedPage, nameof(parsedPage));
			foreach (var node in parsedPage.FindAllRecursive<TemplateNode>(template => template.GetTitleValue() == "Map Link"))
			{
				if (node.FindParameter("ns_base") is ParameterNode parameter)
				{
					var value = parameter.ValueToText()?.Trim();
					if (value == "DB" || value == "Dragonborn")
					{
						parameter.SetName("map");
					}
				}
			}
		}
		#endregion
	}
}
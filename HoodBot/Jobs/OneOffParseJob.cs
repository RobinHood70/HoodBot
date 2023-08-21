namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Parse Job")]
		public OneOffParseJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string? LogDetails => this.EditSummary;

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Remove scrollboxes on short entries";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Scrollbox", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

		protected override void ParseText(ContextualParser parser)
		{
			parser.Replace(node => Replacer(node, parser), false);
			parser.MergeText(true);
			parser.ReplaceText("\n\n\n", "\n\n", System.StringComparison.Ordinal);
		}

		private static NodeCollection? Replacer(IWikiNode node, ContextualParser parser)
		{
			if (node is not SiteTemplateNode template ||
			!template.TitleValue.PageNameEquals("Scrollbox") ||
			template.Find("content") is not IParameterNode contentNode)
			{
				return null;
			}

			var content = contentNode.Value.ToRaw().Trim();
			var contentLines = content.Split(TextArrays.NewLineChars);
			var numOccurrences = (contentLines.Length - 1).ToStringInvariant();
			if (!contentLines[0].Contains("Appearances: " + numOccurrences, System.StringComparison.Ordinal) &&
				!contentLines[0].Contains("Discounts: " + numOccurrences, System.StringComparison.Ordinal))
			{
				Debug.WriteLine("Possible appearances mismatch on " + parser.Page.Title.FullPageName());
			}

			return (contentLines.Length > 5 || content.Length > 1000)
				? null
				: new NodeCollection(parser.Factory)
				{
					parser.Factory.TextNode(content)
				};
		}
		#endregion
	}
}
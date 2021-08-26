namespace RobinHood70.HoodBot.Jobs
{
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Static Fields
		private static readonly Regex DescriptionReplacer = new(@"^:\s*''(?<desc>.+?)''\s*\n?", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
		#endregion

		#region Fields
		private string? oldDescription;
		#endregion

		#region Constructors
		[JobInfo("One-Off Parse Job")]
		public OneOffParseJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Remove redundant description";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online Collectible Summary");

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			if (parsedPage.FindTemplate("Online Collectible Summary") is SiteTemplateNode template &&
				template.Find("description") is IParameterNode desc)
			{
				var parsedDesc = new SiteNodeFactory(this.Site).Parse(this.oldDescription);
				var oldDesc = new WikiPlainTextVisitor(this.Site).Build(parsedDesc);
				var newRaw = desc.Value.ToRaw().Trim();
				var newBytes = Encoding.GetEncoding("Windows-1252").GetBytes(newRaw);
				var newDesc = Encoding.UTF8.GetString(newBytes);
				if (string.Equals(oldDesc, newDesc, System.StringComparison.Ordinal))
				{
					desc.SetValue(this.oldDescription);
				}
				else
				{
					this.WriteLine("* " + parsedPage.Context.AsLink(true) + " - needs checked.");
					if (!string.Equals(newDesc, newRaw, System.StringComparison.Ordinal))
					{
						desc.SetValue(newDesc);
					}
				}
			}
		}

		protected override void ResultsPageLoaded(object sender, Page page)
		{
			var match = DescriptionReplacer.Match(page.Text);
			if (match.Success)
			{
				this.oldDescription = match.Groups["desc"].Value;
				page.Text = page.Text.Remove(match.Index, match.Length);
				base.ResultsPageLoaded(sender, page);
			}
		}
		#endregion
	}
}
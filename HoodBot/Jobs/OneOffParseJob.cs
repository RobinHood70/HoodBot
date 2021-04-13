namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Static Fields
		private static readonly Regex JournalTable = new(@"\{\|.*?\|-\n.*?\|-\n", RegexOptions.Singleline | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Constructors
		[JobInfo("One-Off Parse Job")]
		public OneOffParseJob(JobManager jobManager)
			: base(jobManager) => this.MinorEdit = false;
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Convert to template (bot-assisted)";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			this.Pages.SetLimitations(LimitationType.FilterTo, UespNamespaces.BetterCities, UespNamespaces.OblivionMod);
			this.Pages.GetBacklinks("Template:OB Journal Entries Notes");
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			var header = parsedPage.IndexOfHeader("Journal Entries");
			var nodes = parsedPage.Nodes;
			var journalText = WikiTextVisitor.Raw(nodes.GetRange(header + 1, nodes.Count - header - 1));
			if (JournalTable.Match(journalText) is Match journalMatch && journalMatch.Success)
			{
				journalText = journalText[..journalMatch.Index] + "{{Journal Entries\n" + journalText[(journalMatch.Index + journalMatch.Length)..];
				journalText = journalText
					.Replace("|style=\"text-align:center;\"|", string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("|-\n", string.Empty, StringComparison.Ordinal)
					.Replace("[[File:Check.png|Finishes quest]]", "fin", StringComparison.OrdinalIgnoreCase)
					.Replace("||", "|", StringComparison.Ordinal)
					.Replace("|}", "}}", StringComparison.Ordinal)
					.Replace("| ", "|", StringComparison.Ordinal)
					.Replace(" |", "|", StringComparison.Ordinal)
					.Replace("||", "|   |", StringComparison.Ordinal)
					.Replace("\n\n{{OB", "\n{{OB", StringComparison.Ordinal);

				for (var i = nodes.Count - 1; i > header; i--)
				{
					nodes.RemoveAt(i);
				}

				parsedPage.Nodes.AddRange(new SiteNodeFactory(this.Site).Parse(journalText));
			}
		}

		protected override void ResultsPageLoaded(object sender, Page page)
		{
			page.Text = page.Text
				.Replace(" <br", "<br", StringComparison.OrdinalIgnoreCase)
				.Replace("=\n\n", "=\n", StringComparison.Ordinal)
				.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
			base.ResultsPageLoaded(sender, page);
		}
		#endregion
	}
}
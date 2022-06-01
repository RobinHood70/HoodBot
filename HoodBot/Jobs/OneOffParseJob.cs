namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Parser;
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
		protected override string EditSummary => "Remove empty Houses section from collectibles";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.Pages.Shuffle();
			base.Main();
		}

		protected override void LoadPages() => this.Pages.GetCategoryMembers("Online-Furnishings-Empty House Section");

		protected override void ParseText(object sender, ContextualParser parser)
		{
			var sections = parser.ToSections(2);
			for (var i = sections.Count - 1; i >= 0; i--)
			{
				var section = sections[i];
				var sectionHeader = section.Header?.GetInnerText(true) ?? string.Empty;
				if (string.Equals(sectionHeader, "Houses", StringComparison.Ordinal))
				{
					sections.RemoveAt(i);
				}
			}

			parser.FromSections(sections);
		}
		#endregion
	}
}
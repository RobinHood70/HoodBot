namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Static Fields
		private readonly HashSet<string> achievementHeaders = new(StringComparer.Ordinal) { "Achievement", "Achievements", "Reward", "Rewards" };
		#endregion

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
		protected override string EditSummary => "Remove empty Houses and/or add Achievement/Reward";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() => this.Pages.GetCategoryMembers("Online-Furnishings-Tagged for Bot");

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("Online Furnishing Summary") is not SiteTemplateNode template)
			{
				return;
			}

			var sections = parser.ToSections(2);
			var skip = false;
			for (var i = sections.Count - 1; i >= 0; i--)
			{
				var houseSection = sections[i];
				var sectionHeader = houseSection.Header?.GetTitle(true) ?? string.Empty;
				if (string.Equals(sectionHeader, "Houses", StringComparison.Ordinal))
				{
					sections.RemoveAt(i);
				}

				if (this.achievementHeaders.Contains(sectionHeader))
				{
					skip = true;
				}
			}

			parser.FromSections(sections);

			var insertAt = parser.FindIndex<SiteTemplateNode>(node => node.TitleValue.PageNameEquals("NewLeft"));
			insertAt++;

			string? section = null;
			if (!skip && template.Find("achievement") is IParameterNode achievement)
			{
				var achName = achievement.Value.ToRaw().Trim();
				section = $"\n==Achievement==\nThis furnishing may be purchased after completing the following [[Online:Achievements|achievement]]:\n{{{{ESO Achievements List|{achName}}}}}\n";
			}
			else if (!skip && template.Find("reward") is IParameterNode reward)
			{
				var achName = reward.Value.ToRaw().Trim();
				section = $"\n==Reward==\nThis furnishing is rewarded after completing the following [[Online:Achievements|achievement]]:\n{{{{ESO Achievements List|{achName}}}}}\n";
			}

			if (section is not null)
			{
				parser.InsertRange(insertAt, parser.Parse(section));
			}
		}
		#endregion
	}
}
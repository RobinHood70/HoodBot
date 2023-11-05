namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffTemplateJob : TemplateJob
	{
		#region Constructors
		[JobInfo("One-Off Template Job")]
		public OneOffTemplateJob(JobManager jobManager)
				: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogDetails => "Update " + this.TemplateName;

		public override string LogName => "One-Off Template Job";
		#endregion

		#region Protected Override Properties
		protected override string TemplateName => "Flora Summary";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Harmonize parameters";

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			template.Remove("typenamesp");
			var locs = template.Find("loc");
			var newLocs = new List<string>();
			if (locs is not null)
			{
				foreach (var linkNode in locs.Value.LinkNodes)
				{
					var link = SiteLink.FromLinkNode(this.Site, linkNode);
					if (link.Text is not null)
					{
						newLocs.Add(link.Text);
					}
				}
			}

			if (newLocs.Count > 0)
			{
				template.Remove("loc");
				template.Update("planets", string.Join(", ", newLocs), ParameterFormat.OnePerLine, false);
			}
			else
			{
				template.RenameParameter("loc", "planets");
			}

			var biomes = template.Find("biomes")?.Value.ToRaw();
			if (biomes is not null)
			{
				biomes = biomes
					.Replace("\n", string.Empty, StringComparison.Ordinal)
					.Replace("* ", ", ", StringComparison.Ordinal)
					.Replace("*", ", ", StringComparison.Ordinal)
					.Replace(",,", ",", StringComparison.Ordinal)
					.Trim(',')
					.Trim()
					.Replace("{{Huh}}", string.Empty, StringComparison.Ordinal);
				template.Update("biomes", biomes, ParameterFormat.OnePerLine, false);
			}
		}
		#endregion
	}
}
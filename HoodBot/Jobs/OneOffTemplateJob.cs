namespace RobinHood70.HoodBot.Jobs
{
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

		#region Public Properties
		public override string? LogDetails => "Update " + this.TemplateName;

		public override string LogName => "One-Off Template Job";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Update for changes in template";

		protected override string TemplateName => "ESO Contraband Item";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			var value = template.Find("value");
			if (template.Find(5) is IParameterNode quality)
			{
				var qualityValue = quality.Value.ToRaw();
				if (string.Equals(qualityValue, "2", System.StringComparison.Ordinal))
				{
					quality.SetValue("f", ParameterFormat.NoChange);
				}
				else if (
					value is not null &&
					(string.Equals(qualityValue, "l", System.StringComparison.Ordinal) ||
					string.Equals(qualityValue, "t", System.StringComparison.Ordinal)) &&
					string.Equals(value.Value.ToRaw(), "0", System.StringComparison.Ordinal))
				{
					template.Remove("value");
				}
			}

			if (value is not null)
			{
				if (value.Value.Find<SiteTemplateNode>(n => string.Equals(n.GetTitleText(), "ESO Price", System.StringComparison.Ordinal)) is SiteTemplateNode price && price.Find(1) is IParameterNode value2)
				{
					value.Value.Clear();
					value.Value.AddRange(value2.Value);
				}
			}
		}

		protected override void PageLoaded(object sender, Page page)
		{
			page.Text = page.Text
				.Replace("{{icon|", "{{subst:icon|", System.StringComparison.Ordinal);
			base.PageLoaded(sender, page);
		}
		#endregion
	}
}
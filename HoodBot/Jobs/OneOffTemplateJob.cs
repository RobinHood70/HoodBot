namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("One-Off Template Job")]
	public class OneOffTemplateJob(JobManager jobManager) : TemplateJob(jobManager)
	{
		#region Public Override Properties
		public override string LogDetails => "Update " + this.TemplateName;

		public override string LogName => "One-Off Template Job";
		#endregion

		#region Protected Override Properties
		protected override string TemplateName => "Planet Infobox";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Use orbital_position instead of order";

		protected override void ParseTemplate(SiteTemplateNode template, SiteParser parser)
		{
			var op = template.Find("orbital_position");
			if (op is not null && op.Value.ToRaw().Trim().Length > 0)
			{
				template.Remove("order");
			}
			else
			{
				var order = template.Find("order");
				if (op is not null && order is not null)
				{
					op.Value.Clear();
					op.Value.AddRange(order.Value);
					template.Remove("order");
				}
				else
				{
					template.RenameParameter("order", "orbital_position");
				}
			}
		}
		#endregion
	}
}
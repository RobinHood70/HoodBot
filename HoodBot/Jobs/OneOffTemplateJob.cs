namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffTemplateJob : TemplateJob
	{
		#region Fields
		private static readonly Dictionary<string, string> RomanToNum = new(System.StringComparer.Ordinal)
		{
			["I"] = "1",
			["II"] = "2",
			["III"] = "3",
			["IV"] = "4",
			["V"] = "5",
			["VI"] = "6",
			["VII"] = "7",
			["VIII"] = "8",
			["IX"] = "9",
			["X"] = "10",
		};
		#endregion

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
		protected override string TemplateName => "Planet Infobox";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Use orbital_position instead of order";

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
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
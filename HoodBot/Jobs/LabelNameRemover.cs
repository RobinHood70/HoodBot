﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class LabelNameRemover : TemplateJob
	{
		#region Constructors
		[JobInfo("Label Name Fixer")]
		public LabelNameRemover(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Remove redundant imgdesc";

		protected override string TemplateName => "Online Collectible Summary";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			if (template.Find("titlename") != null)
			{
				Debug.WriteLine(parser.Page.FullPageName);
			}
			else if (string.Equals(
				template.GetValue("imgdesc"),
				parser.Page.LabelName(),
				StringComparison.Ordinal))
			{
				template.Remove("imgdesc");
			}
		}
		#endregion
	}
}

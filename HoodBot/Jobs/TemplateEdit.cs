namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class TemplateEdit : TemplateJob
	{
		#region Static Fields
		private static readonly char[] Trimmables = new char[] { ' ', '(', ')', '\r', '\n' };
		#endregion

		#region Constructors
		[JobInfo("Template Edit")]
		public TemplateEdit(JobManager jobManager)
				: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Switch materials and skills to tildes";

		protected override string TemplateName => "Online Furnishing Summary";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			ReplaceParamValue(template, "materials");
			ReplaceParamValue(template, "skills");
		}
		#endregion

		#region Private Static Methods
		private static void ReplaceParamValue(SiteTemplateNode template, string search)
		{
			if (template.Find(search) is IParameterNode param)
			{
				List<string> newEntries = new();
				var entries = param
					.Value
					.ToRaw()
					.Replace(
						"Ivory, Polished",
						"Ivory~ Polished",
						StringComparison.OrdinalIgnoreCase)
					.Split(TextArrays.Comma);
				foreach (var entry in entries)
				{
					newEntries.Add(entry.Trim(Trimmables));
				}

				param.SetValue(string.Join("~", newEntries));
			}
		}
		#endregion
	}
}

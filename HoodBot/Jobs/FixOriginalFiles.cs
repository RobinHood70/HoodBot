namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

public class FixOriginalFiles : TemplateJob
{
	#region Constructors

	[JobInfo("Fix originalfile names")]
	public FixOriginalFiles(JobManager jobManager)
		: base(jobManager)
	{
		this.Shuffle = true;
	}
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Update " + this.TemplateName;

	public override string LogName => "One-Off Template Job";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "Online File";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Fix originalfile name";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (template.Find("originalfile") is not IParameterNode filename)
		{
			return;
		}

		var paramValue = filename.GetRaw();
		if (!parser.Title.PageNameEquals("ON-icon-store-Sunken_Trove_Crown_Crate.png") &&
			(paramValue.Contains('[', StringComparison.Ordinal) ||
			paramValue.Contains(']', StringComparison.Ordinal)))
		{
			this.StatusWriteLine("Malformed originalfile on " + parser.Title.FullPageName());
		}

		if (paramValue.Length > 0 && paramValue[0] == '/')
		{
			paramValue = paramValue[1..];
		}

		if (paramValue.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
		{
			paramValue = paramValue[..^4];
		}

		var pageName = parser.Page.Title.PageName;
		if ((pageName.Contains("-icon-armor-", StringComparison.Ordinal) || pageName.Contains("-icon-weapon-", StringComparison.Ordinal)) &&
			!paramValue.Contains('/', StringComparison.Ordinal))
		{
			paramValue = "esoui/art/icons/" + paramValue;
		}

		filename.SetValue(paramValue, ParameterFormat.Copy);
	}
	#endregion
}
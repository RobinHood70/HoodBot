namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("Missing NPC Images", "Maintenance")]
public class MissingNPCImages(JobManager jobManager) : TemplateJob(jobManager)
{
	#region Fields
	private readonly TitleCollection allFiles = new(jobManager.Site);
	#endregion

	#region Public Override Properties
	public override string LogName => "One-Off Template Job";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "NPC Summary";
	#endregion

	#region Protected Override Methods

	protected override void BeforeLoadPages()
	{
		this.StatusWriteLine("Getting file names");
		this.allFiles.GetNamespace(MediaWikiNamespaces.File);
	}

	protected override string GetEditSummary(Page page) => "Comment out missing images";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (template.Find("image") is IParameterNode image)
		{
			var value = image.GetRaw();
			if (value.Length > 0 &&
				!value.StartsWith("<!--", StringComparison.Ordinal) &&
				!value.OrdinalICEquals("none"))
			{
				if (!this.allFiles.Contains("File:" + value))
				{
					image.SetValue("<!--" + value + "-->", ParameterFormat.Verbatim);
				}
			}
		}
	}
	#endregion
}
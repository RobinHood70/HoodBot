namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Template Job")]
public class OneOffTemplateJob(JobManager jobManager) : TemplateJob(jobManager)
{
	#region Static Fields
	private static readonly HashSet<string> SeasonVariants = new(StringComparer.OrdinalIgnoreCase)
	{
		"2025 Content Pass", "Seasons of the Worm Cult", "Solstice"
	};
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Update " + this.TemplateName;

	public override string LogName => "One-Off Template Job";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "Livre de jeu";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Corriger l'icône";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		foreach (var link in parser.LinkNodes)
		{
			var linkText = link.ToRaw();
			if (linkText.Contains("File:", StringComparison.OrdinalIgnoreCase))
			{
				Debug.WriteLine(parser.Title + ": " + linkText);
			}
		}

		if (template.Find("scroll") is null)
		{
			return;
		}

		if (template.Find("icon") is IParameterNode icon)
		{
			var text = icon.Value.ToRaw().Trim();
			switch (text[3..].ToLowerInvariant())
			{
				case "tx_paper_plain_01.png":
					icon.Value.Clear();
					icon.Value.AddText("MW-icon-book-Plain1.png\n");
					break;
				case "tx_scroll_open_01.png":
					icon.Value.Clear();
					icon.Value.AddText("MW-icon-book-RolledPaper1.png\n");
					break;
				default:
					if (!text.Contains("-icon-", StringComparison.Ordinal))
					{
						Debug.WriteLine(text);
					}

					break;
			}
		}
	}
	#endregion
}
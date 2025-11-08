namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class OneOffParseJob : ParsedPageJob
{
	#region Static Fields
	private static readonly HashSet<string> Replacements = new(StringComparer.OrdinalIgnoreCase)
	{
		"Burning",
		"Chilled",
		"Concussion",
		"Diseased",
		"Hemorrhaging",
		"Overcharged",
		"Poisoned",
		"Sundered",
	};
	#endregion

	#region Constructors
	[JobInfo("One-Off Parse Job")]
	public OneOffParseJob(JobManager jobManager)
		: base(jobManager)
	{
	}
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Replace links";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => this.LogDetails;

	protected override void LoadPages() => this.Pages.GetBacklinks("ON:Combat", BacklinksTypes.Backlinks);

	protected override void ParseText(SiteParser parser)
	{
		for (var i = parser.Count - 1; i >= 0; i--)
		{
			var node = parser[i];
			if (node is ILinkNode linkNode && linkNode.GetTitle(this.Site) == "ON:Combat")
			{
				var siteLink = SiteLink.FromLinkNode(this.Site, linkNode);
				if (siteLink.Fragment is not null && Replacements.Contains(siteLink.Fragment))
				{
					var ns = siteLink.OriginalTitle!.Split(TextArrays.Colon, 2)[0];
					parser[i] = parser.Factory.LinkNodeFromParts($"{ns}:{siteLink.Fragment}", siteLink.Text ?? string.Empty);
				}
			}
		}
	}
	#endregion
}
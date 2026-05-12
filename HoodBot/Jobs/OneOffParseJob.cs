namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update Daggerfall Preview text";

	protected override void LoadPages()
	{
		var nses = new List<int>();
		foreach (var ns in this.Site.Namespaces)
		{
			if (ns.Id < 300 && !ns.IsTalkSpace)
			{
				nses.Add(ns.Id);
			}
		}

		this.Pages.GetSearchResults("Daggerfall Preview", WhatToSearch.Text, nses);
		this.Pages.GetSearchResults("Daggerfall_Preview", WhatToSearch.Text, nses);
		this.Pages.Remove("UESPWiki:Administrator Noticeboard/Archives/Images");
	}

	protected override void ParseText(SiteParser parser)
	{
		foreach (var textNode in parser.TextNodes)
		{
			textNode.Text = textNode.Text.Replace("Daggerfall Preview", "Daggerfall Interactive Preview", StringComparison.Ordinal);
			textNode.Text = textNode.Text.Replace("Daggerfall_Preview", "Daggerfall_Interactive_Preview", StringComparison.Ordinal);
		}
	}
	#endregion
}
namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.WikiCommon;

[method: JobInfo("Case-Sensitive Search")]
internal sealed class CaseSensitiveSearch(JobManager jobManager, string search) : WikiJob(jobManager, JobType.ReadOnly)
{
	protected override void Main()
	{
		var subjectSpaces = new List<int>();
		foreach (var ns in this.Site.Namespaces)
		{
			if (ns.IsSubjectSpace && ns.CanTalk)
			{
				subjectSpaces.Add(ns.Id);
			}
		}

		this.WriteLine("== Case-Sensitive Search Term: " + search + " ==");
		var pages = new PageCollection(this.Site);
		pages.GetSearchResults(search, WhatToSearch.Title | WhatToSearch.Text, subjectSpaces);
		pages.Sort();
		foreach (var page in pages)
		{
			if (page.Text.Contains(search, StringComparison.Ordinal))
			{
				var siteLink = new SiteLink(page.Title);
				this.WriteLine("* " + siteLink.AsLink(LinkFormat.ForcedLink));
			}
		}
	}
}
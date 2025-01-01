namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
{
	#region Public Override Properties
	public override string LogName => "One-Off Job";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update project name";

	protected override void LoadPages()
	{
		var namespaces = new Uesp.UespNamespaceList(this.Site);
		var gamespaces = new List<int>(namespaces.Count);
		foreach (var ns in namespaces)
		{
			if (ns.Value.IsGamespace)
			{
				gamespaces.Add(ns.Value.BaseNamespace.Id);
			}
		}

		this.Pages.GetSearchResults("\"Province: Cyrodiil\"", WhatToSearch.Text, gamespaces);
		this.Pages.GetSearchResults("insource:\"Province: Cyrodiil\"", WhatToSearch.Text, gamespaces);
	}

	protected override void PageLoaded(Page page) => page.Text = page.Text.Replace("Province: Cyrodiil", "Project Cyrodiil", System.StringComparison.OrdinalIgnoreCase);
	#endregion
}
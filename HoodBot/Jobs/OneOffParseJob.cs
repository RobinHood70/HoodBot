namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class OneOffParseJob : ParsedPageJob
{
	private static readonly Regex EmDashes = new(@"\s*—\s*", RegexOptions.None, Globals.DefaultRegexTimeout);

	[JobInfo("One-Off Parse Job")]
	public OneOffParseJob(JobManager jobManager)
		: base(jobManager)
	{
	}

	protected override string GetEditSummary(Page page) => "Replace em dashes";

	protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online NPC Summary", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

	protected override void ParseText(SiteParser parser)
	{
		foreach (var template in parser.FindTemplates("Online NPC Summary"))
		{
			foreach (var parameter in template.Parameters)
			{
				foreach (var node in parameter.Value)
				{
					if (node is ITextNode textNode && textNode.Text.Contains('—', StringComparison.Ordinal))
					{
						textNode.Text = EmDashes.Replace(textNode.Text, ", ");
						Debug.WriteLine($"{parser.Title}\t{parameter.ToRaw().Trim()}");
					}
				}
			}
		}
	}
}
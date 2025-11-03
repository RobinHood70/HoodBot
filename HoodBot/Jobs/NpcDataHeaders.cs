namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WikiCommon;

[method: JobInfo("NPC Data Headers", "Morrowind")]
internal sealed class NpcDataHeaders(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var factionFaker = new Regex("Class *[\n!]! *Level", RegexOptions.None, Globals.DefaultRegexTimeout);
		var headerFinder = new Regex(@"\{\|.*?\n(?<headers>(!.*?\n)+?).*?\{\{[nN]PC[ _]Data", RegexOptions.None, Globals.DefaultRegexTimeout);
		var pages = new PageCollection(this.Site);
		pages.GetBacklinks("Template:NPC Data", BacklinksTypes.EmbeddedIn);
		foreach (var page in pages)
		{
			page.Text = factionFaker.Replace(page.Text, "Class !! Faction !! Level");
			IReadOnlyList<Match> matches = headerFinder.Matches(page.Text);
			foreach (var match in matches)
			{
				var headers = match.Groups["headers"].Value.Trim();
				headers = headers[1..]
					.Replace("\n!", "!!", StringComparison.Ordinal)
					.Replace("||", "!!", StringComparison.Ordinal);
				var split = headers.Split("!!", StringSplitOptions.TrimEntries);
				headers = string.Join('\t', split);
				if (true || headers.Length != 0)
				{
					Debug.WriteLine($"{page.Title}\t{headers}");
				}
			}
		}
	}
	#endregion
}
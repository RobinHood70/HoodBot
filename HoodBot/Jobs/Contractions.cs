namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;

[method: JobInfo("Contractions")]
internal sealed class Contractions(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Static Fields
	private static readonly Regex ContractionFinder = new(@"\b(aren[’']t|can[’']t|couldn[’']t|didn[’']t|doesn[’']t|don[’']t|hadn[’']t|hasn[’']t|haven[’']t|he[’']d|he[’']ll|he[’']s|I[’']d|I[’']ll|I[’']m|I[’']ve|isn[’']t|let[’']s|mightn[’']t|mustn[’']t|shan[’']t|she[’']d|she[’']ll|she[’']s|shouldn[’']t|that[’']s|there[’']s|they[’']d|they[’']ll|they[’']re|they[’']ve|we[’']d|we[’']ll|we[’']re|we[’']ve|weren[’']t|what[’']ll|what[’']re|what[’']s|what[’']ve|where[’']s|who[’']d|who[’']ll|who[’']re|who[’']s|who[’']ve|won[’']t|wouldn[’']t|you[’']d|you[’']ll|you[’']re|you[’']ve)\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		var nsMeta = new UespNamespaceList(this.Site);
		var ignore = new TitleCollection(this.Site);
		var realNamespaces = new HashSet<Namespace>();
		this.ProgressMaximum = nsMeta.Count;
		foreach (var ns in nsMeta)
		{
			var baseNamespace = ns.Value.BaseNamespace;
			if (baseNamespace.IsContentSpace)
			{
				var cat = ns.Value.Category;
				ignore.GetCategoryMembers(cat + "-Books", false);
				ignore.GetCategoryMembers(cat + "-NPCs", false);
				ignore.GetCategoryMembers(cat + "-Quests", false);
				realNamespaces.Add(baseNamespace);
			}

			this.Progress++;
		}

		var titles = new TitleCollection(this.Site);
		//// realNamespaces.Clear();
		//// realNamespaces.Add(this.Site[UespNamespaces.Lore]);
		this.ResetProgress(realNamespaces.Count);
		foreach (var ns in realNamespaces)
		{
			titles.GetNamespace(ns.Id, Filter.Exclude);
			this.Progress++;
		}

		this.Progress = 0;
		var hash = new HashSet<Title>(titles);
		hash.ExceptWith(ignore);
		this.StatusWriteLine($"Loading {hash.Count} pages");

		var pages = new PageCollection(this.Site);
		pages.GetTitles(hash);
		pages.Sort();

		this.StatusWriteLine("Searching");
		foreach (var page in pages)
		{
			this.SearchPage(page);
		}
	}
	#endregion

	#region Private Methods
	private void SearchPage(Page page)
	{
		var parser = new SiteParser(page);
		var text = new List<string>();
		foreach (var textNode in parser.TextNodes)
		{
			var matches = ContractionFinder.Matches(textNode.Text);
			if (matches.Count > 0)
			{
				foreach (var match in (IEnumerable<Match>)matches)
				{
					text.Add(match.Value);
				}
			}
		}

		if (text.Count > 0)
		{
			this.WriteLine($"* {SiteLink.ToText(page)}");
			this.WriteLine(':' + string.Join(", ", text));
		}
	}
	#endregion
}
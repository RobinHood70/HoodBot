namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	public class MerReplacer : EditJob
	{
		#region Static Fields
		private static readonly Dictionary<string, string> MerText = new(StringComparer.Ordinal)
		{
			["Altmer"] = "High Elf",
			["Bosmer"] = "Wood Elf",
			["Dunmer"] = "Dark Elf",
			["High Elf"] = "High Elf",
			["Wood Elf"] = "Wood Elf",
			["Dark Elf"] = "Dark Elf",
		};
		#endregion

		#region Fields
		private readonly TitleCollection merPages;
		private readonly UespNamespaceList nsList;
		#endregion

		#region Constructors
		[JobInfo("Mer Replacer")]
		public MerReplacer(JobManager jobManager)
			: base(jobManager)
		{
			this.nsList = new UespNamespaceList(this.Site);
			this.merPages = new TitleCollection(this.Site)
			{
				"Morrowind:Dark Elf",
				"Morrowind:High Elf",
				"Morrowind:Wood Elf",
				"Blades:Dark Elf",
				"Blades:High Elf",
				"Blades:Wood Elf",
				"Oblivion:Dark Elf",
				"Oblivion:High Elf",
				"Oblivion:Wood Elf",
				"Skyrim:Dark Elf",
				"Skyrim:High Elf",
				"Skyrim:Wood Elf",
				"Online:Dark Elf",
				"Online:High Elf",
				"Online:Wood Elf",
				"Legends:Dark Elf",
				"Legends:High Elf",
				"Legends:Wood Elf"
			};
		}
		#endregion

		#region Proteced Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Getting mer pages");
			PageCollection pages = PageCollection.Unlimited(this.Site, PageModules.Backlinks, false);
			pages.GetTitles(this.merPages);

			this.StatusWriteLine("Getting backlinks");
			TitleCollection backTitles = new(this.Site);
			foreach (var page in pages)
			{
				foreach (var backlink in page.Backlinks)
				{
					if ((backlink.Value & (BacklinksTypes.Backlinks | BacklinksTypes.ImageUsage)) != 0 && UespNamespaces.IsGamespace(backlink.Key.Namespace.Id))
					{
						backTitles.Add(backlink.Key);
					}
				}
			}

			this.StatusWriteLine("Loading pages");
			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.Pages.GetTitles(backTitles);
			this.Pages.PageLoaded -= this.Pages_PageLoaded;

			this.StatusWriteLine("Parsing");
			SortedSet<string> results = new(StringComparer.Ordinal);
			this.Pages.Sort();
			foreach (var page in this.Pages)
			{
				ContextualParser parsedPage = new(page);
				foreach (var link in parsedPage.FindAll<SiteLinkNode>())
				{
					var pageName = link.TitleValue.PageName;
					if (this.merPages.Contains(link.TitleValue))
					{
						if (SiteLink.FromLinkNode(this.Site, link) is var siteLink &&
							siteLink.Text != null &&
							MerText.TryGetValue(siteLink.Text, out var linkPage) &&
							siteLink.PageNameEquals(linkPage))
						{
							results.Add($"* {page.AsLink()}: {siteLink} may need adjusted.");
						}
					}
				}
			}

			foreach (var result in results)
			{
				this.WriteLine(result);
			}
		}

		protected override void Main() => this.SavePages("Fix mer text", true, this.Pages_PageLoaded);
		#endregion

		#region Private Methods
		private void Pages_PageLoaded(object sender, Page page)
		{
			Regex replacer = new(@"(?<pretext>(''')?" + Regex.Escape(page.PageName) + @"(''')?,?(\s+(are|is))?\s+an?(\s+\w+)?\s+)((?<race>(Altmer|Bosmer|Dunmer|Dark Elf|High Elf|Wood Elf))|\[\[(?<ns>[^:]*?::?)(Dark|High|Wood)\ Elf\|(?<race>(Altmer|Bosmer|Dunmer|Dark Elf|High Elf|Wood Elf))\]\])", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			page.Text = replacer.Replace(page.Text, match => this.PhraseReplace(page, match));
		}

		private string PhraseReplace(Page page, Match match)
		{
			var race = match.Groups["race"].Value;
			var nsText = match.Groups["ns"];
			var ns = nsText.Success ? nsText.Value.TrimEnd(TextArrays.Colon) : this.nsList.FromTitle(page)?.Parent.CanonicalName;
			var raceLink = MerText[race];
			var retval = $"{match.Groups["pretext"].Value}[[{ns}:{raceLink}|{raceLink}]]";
			return retval
				.Replace("  ", " ", StringComparison.Ordinal)
				.Replace("an [[", "a [[", StringComparison.Ordinal);
		}
		#endregion
	}
}

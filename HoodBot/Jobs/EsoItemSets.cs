namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;

	internal class EsoItemSets : EditJob
	{
		#region Static Fields
		private static readonly Regex OnlineUpdateRegex = Template.Find("Online Update");
		private static readonly Regex SetBonusRegex = new Regex(@"\([1-6] items?\)[^(]*(\s|\n)*");
		private static readonly Uri SetSummaryPage = new Uri("http://esolog.uesp.net/viewlog.php?record=setSummary&format=csv");
		private static readonly string[] ItemSeparator = new[] { "(", ") ", "\n" };
		#endregion

		#region Fields
		private readonly CsvFile parser = new CsvFile();
		private readonly HoodBotFunctions botFunctions;
		private readonly Dictionary<string, PageData> data = new Dictionary<string, PageData>();
		private PageCollection pages;
		#endregion

		#region Constructors
		[JobInfo("Item Sets", "ESO")]
		public EsoItemSets(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.botFunctions = site.UserFunctions as HoodBotFunctions;
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO Item Sets";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.StatusWriteLine("Saving pages");
			this.EditConflictAction = this.SetLoaded;
			this.pages.Sort();
			foreach (var page in this.pages)
			{
				this.SavePage(page, this.LogName, false);
				this.Progress++;
			}

			EsoGeneral.SetBotUpdateVersion(this, "itemset");
			this.Progress++;
		}

		protected override void OnCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.OnCompleted();
		}

		protected override void PrepareJob()
		{
			EsoReplacer.Initialize(this);
			this.StatusWriteLine("Fetching data");
			var csvData = this.botFunctions.NativeClient.Get(SetSummaryPage);
			this.parser.ReadText(csvData, true);
			this.ProgressMaximum = this.parser.Count + 5;
			this.Progress = 3;

			this.StatusWriteLine("Updating");
			var titles = new TitleCollection(this.Site);
			foreach (var row in this.parser)
			{
				var setName = row["setName"].Replace(@"\'", "'");
				if (ReplacementData.SetNameFixes.TryGetValue(setName, out var pageName))
				{
					this.Warn($"Set replacement made: {setName} => {pageName}");
				}

				pageName = "Online:" + (pageName ?? setName);
				titles.Add(pageName);
				var bonusDescription = row["setBonusDesc"];
				if (bonusDescription[0] != '(')
				{
					this.Warn($"Set bonus for {pageName} doesn't start with a bracket:{Environment.NewLine}{bonusDescription}");
				}

				this.data.Add(pageName, new PageData(setName, bonusDescription));
			}

			this.pages = new PageCollection(this.Site);
			this.pages.PageLoaded += this.SetLoaded;
			this.pages.GetTitles(titles);
			this.pages.PageLoaded -= this.SetLoaded;
			this.GenerateReport();
			this.Progress++;
		}
		#endregion

		#region Private Methods
		private static string BuildNewPage(string setName)
		{
			var sb = new StringBuilder();
			sb
				.Append("{{Trail|Sets}}{{Online Update}}{{Minimal}}\n")
				.Append("'''").Append(setName).Append("''' is a {{huh}}-rank [[Online:Sets|item set]] found in {{huh}}.\n\n")
				.Append("Set pieces are {{huh}} style in {{huh}}.\n\n")
				.Append("===Bonuses===\n")
				.Append("<onlyinclude>\n")
				.Append("</onlyinclude>\n")
				.Append("===Pieces===\n")
				.Append("The set consists of {{huh}}. For detailed weapon stats and set values, see individual item pages by clicking on the links below.\n")
				.Append("<!--\n")
				.Append("* {{Item Link|id=|}}\n")
				.Append("* {{Item Link|id=|}}\n")
				.Append("-->\n")
				.Append("===Notes===\n\n")
				.Append("{{Online Sets}}\n")
				.Append("{{ESO Sets With|subtype=|source=}}\n")
				.Append("{{Stub|Item Set}}");

			return sb.ToString();
		}

		private void GenerateReport()
		{
			this.WriteLine("== Item Sets With Non-Trivial Updates ==");
			foreach (var item in this.data)
			{
				if (item.Value.IsNonTrivial)
				{
					this.WriteLine($"* {{{{Pl|{item.Key}|{item.Value.SetName}|diff=cur}}}}");
				}
			}

			this.WriteLine();
		}

		private string PatchNumberReplacer(Match match)
		{
			var template = Template.Parse(match.Value);
			template.RemoveDuplicates();
			template.Remove("update");
			template.Remove("1");
			template.AddOrChange("type", "itemset");

			return template.ToString();
		}

		private void SetLoaded(object sender, Page page)
		{
			var pageData = this.data[page.FullPageName];
			if (!page.Exists || string.IsNullOrEmpty(page.Text))
			{
				page.Text = BuildNewPage(pageData.SetName);
			}

			this.UpdatePageText(page, pageData);
		}

		private void UpdatePageText(Page page, PageData pageData)
		{
			const string marker = "<onlyinclude>";
			const string terminator = "</onlyinclude>";

			var start = page.Text.IndexOf(marker, StringComparison.Ordinal);
			var end = start >= 0 ? page.Text.IndexOf(terminator, start, StringComparison.Ordinal) : -1;
			if (start < 0 || end < 0)
			{
				this.Warn($"Delimiters not found on page {page.FullPageName}");
				return;
			}

			start += marker.Length;
			var sb = new StringBuilder();
			sb.Append('\n');
			var items = SetBonusRegex.Matches(pageData.BonusDescription);
			foreach (Match item in items)
			{
				if (item.Length > 0)
				{
					var split = item.Value.Split(ItemSeparator, StringSplitOptions.RemoveEmptyEntries);
					sb.Append($"'''{split[0]}''': ");
					split[1] = EsoReplacer.ReplaceGlobal(split[1], null);
					split[1] = EsoReplacer.ReplaceLink(split[1]);
					sb.Append(split[1]);

					for (var i = 2; i < split.Length; i++)
					{
						var splitItem = split[i];
						sb.Append("; ");
						splitItem = char.ToLower(splitItem[0], this.Site.Culture) + splitItem.Substring(1);
						splitItem = EsoReplacer.ReplaceGlobal(splitItem, null);
						splitItem = EsoReplacer.ReplaceLink(splitItem);
						sb.Append(splitItem);
					}

					sb.Append("<br>\n");
				}
			}

			sb.Remove(sb.Length - 5, 4);

			var text = sb.ToString();
			pageData.IsNonTrivial = EsoReplacer.CompareReplacementText(this, page.Text.Substring(start, end - start), text, page.FullPageName);
			text = EsoReplacer.FirstLinksOnly(this.Site, text);

			page.Text = OnlineUpdateRegex.Replace(page.Text.Substring(0, start), this.PatchNumberReplacer) + text + page.Text.Substring(end);
		}
		#endregion

		#region Private Classes
		private class PageData
		{
			public PageData(string setName, string bonusDescription)
			{
				this.SetName = setName;
				this.BonusDescription = bonusDescription;
			}

			public string BonusDescription { get; }

			public bool IsNonTrivial { get; set; }

			public string SetName { get; }
		}
		#endregion
	}
}

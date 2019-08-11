﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text.RegularExpressions;
	using System.Windows.Documents;
	using Newtonsoft.Json;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;

	public class AddDeckCodes : EditJob
	{
		#region Fields
		private readonly Regex cardSummaryFinder = Template.Find("Legends Card Summary");
		private PageCollection pages;
		#endregion

		#region Constructors
		[JobInfo("Add Deck Codes", "Legends")]
		public AddDeckCodes([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Add deck codes";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			foreach (var page in this.pages)
			{
				this.SavePage(page, "Add deck code", true);
				this.Progress++;
			}
		}

		protected override void PrepareJob()
		{
			var codeData = JsonConvert.DeserializeObject<List<DeckCodeInfo>>(File.ReadAllText(@"D:\Users\rmorl\Desktop\export_codes.json"));
			var lookup = new Dictionary<Title, DeckCodeInfo>();
			var titles = new TitleCollection(this.Site);
			var ignored = new List<DeckCodeInfo>();
			foreach (var item in codeData)
			{
				if (item.Name == item.TypeName)
				{
					var title = new Title(this.Site.Namespaces[UespNamespaces.Legends], item.Name);
					titles.Add(title);
					lookup.Add(title, item);
				}
				else
				{
					ignored.Add(item);
				}
			}

			ignored.Sort((x, y) => StringComparer.Ordinal.Compare(x.Name, y.Name));
			foreach (var item in ignored)
			{
				this.WriteLine($"* {item.Name} ignored because name doesn't match type {item.TypeName} (deckcode={item.DeckCode})");
			}

			this.pages = titles.Load(new PageLoadOptions(PageModules.Default) { FollowRedirects = true });
			foreach (var title in titles)
			{
				var item = lookup[title];
				var page = this.pages[title.FullPageName];
				if (page != null && !page.IsMissing)
				{
					page.Text = this.cardSummaryFinder.Replace(page.Text, (match) => this.CardSummary_Replacer(match, item.DeckCode), 1);
				}
				else
				{
					this.WriteLine($"* {title.PageName} not found (deckcode={item.DeckCode}).");
				}
			}

			this.ProgressMaximum = this.pages.Count;
			this.pages.Sort();
		}
		#endregion

		#region Private Methods
		private string CardSummary_Replacer(Match match, string deckCode)
		{
			var template = Template.Parse(match.Value);
			template.AddOrChange("deckcode", deckCode);
			return template.ToString();
		}
		#endregion

		#region Private Classes
		private class DeckCodeInfo
		{
			[JsonProperty("english_name")]
			public string Name { get; set; }

			[JsonProperty("collection")]
			public string Collection { get; set; }

			[JsonProperty("export_code")]
			public string DeckCode { get; set; }

			[JsonProperty("type_name")]
			public string TypeName { get; set; }

			public override string ToString() => this.Name;
		}
		#endregion
	}
}

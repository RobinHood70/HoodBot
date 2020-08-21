namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class AddDeckCodes : EditJob
	{
		#region Fields
		private readonly Regex cardSummaryFinder = Template.Find("Legends Card Summary");
		#endregion

		#region Constructors
		[JobInfo("Add Deck Codes", "Legends")]
		public AddDeckCodes([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.Pages.LoadOptions = new PageLoadOptions(PageModules.Default, true);
		#endregion

		#region Public Override Properties
		public override string LogName => "Add deck codes";
		#endregion

		#region Protected Override Methods
		protected override void Main() => this.SavePages("Add deck code");

		protected override void BeforeLogging()
		{
			var codeData = JsonConvert.DeserializeObject<List<DeckCodeInfo>>(File.ReadAllText(@"D:\Users\rmorl\Desktop\export_codes.json"));
			var lookup = new Dictionary<Title, DeckCodeInfo>();
			var titles = new TitleCollection(this.Site);
			var ignored = new List<DeckCodeInfo>();
			foreach (var item in codeData)
			{
				if (item.Name != null)
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
			}

			ignored.Sort((x, y) => StringComparer.Ordinal.Compare(x.Name, y.Name));
			foreach (var item in ignored)
			{
				this.WriteLine($"* {item.Name} ignored because name doesn't match type {item.TypeName} (deckcode={item.DeckCode})");
			}

			this.Pages.GetTitles(titles);
			foreach (var title in titles)
			{
				var item = lookup[title];
				var page = this.Pages[title.FullPageName];
				if (page != null && !page.IsMissing && item.DeckCode != null)
				{
					page.Text = this.cardSummaryFinder.Replace(page.Text, (match) => CardSummary_Replacer(match, item.DeckCode), 1);
				}
				else
				{
					this.WriteLine($"* {title.PageName} not found (deckcode={item.DeckCode}).");
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static string CardSummary_Replacer(Match match, string deckCode)
		{
			var template = Template.Parse(match.Value);
			template.AddOrChange("deckcode", deckCode);
			return template.ToString();
		}
		#endregion

		#region Private Classes
		[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Auto-populated by JSON deserializer.")]
		private class DeckCodeInfo
		{
			[JsonProperty("english_name")]
			public string? Name { get; set; }

			[JsonProperty("collection")]
			public string? Collection { get; set; }

			[JsonProperty("export_code")]
			public string? DeckCode { get; set; }

			[JsonProperty("type_name")]
			public string? TypeName { get; set; }

			public override string ToString() => this.Name ?? FallbackText.Unknown;
		}
		#endregion
	}
}

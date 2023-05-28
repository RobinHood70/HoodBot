namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Static Fields
		private static readonly Regex AttribRegex = new(@"(?<key>(begin|end))=(?<quote>['""]?)(?<value>.*)\k<quote>\s*$", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly HashSet<string> KnownTypes = new(StringComparer.Ordinal) { "Artwork", "ChildrensToys", "Children's Toys", "Cosmetics", "Devices", "DishesCookware", "Dishes and Cookware", "Dolls", "Drinkware", "DryGoods", "Dry Goods", "DryGoods", "FishingSupplies", "Furnishings", "Games", "GroomingItems", "Grooming Items", "Justice", "Lights", "MagicCuriosities", "Magic Curiosities", "Maps", "MedicalSupplies", "MusicalInstruments", "None", "Oddities", "Relic", "Relics", "RitualObjects", "Ritual Objects", "ScrivenerSupplies", "SmithingEquipment", "Smithing Equipment", "Statues", "Tools", "Tribute", "TriflesOrnament", "TriflesOrnaments", "Trifles and Ornaments", "Utensils", "WallDécor", "WardrobeAccessories", "Wardrobe Accessories", "Writings" };
		private static readonly Dictionary<string, string> Zones = new(StringComparer.Ordinal)
		{
			["Blackwood"] = "Blackwood",
			["ClockworkCity"] = "Clockwork City",
			["Clockwork City"] = "Clockwork City",
			["Deadlands"] = "The Deadlands",
			["Elsweyr"] = "Elsweyr",
			["Galen"] = "Galen",
			["GoldCoast"] = "Gold Coast",
			["Hew'sBane"] = "Hew's Bane",
			["High Isle"] = "High Isle",
			["Murkmire"] = "Murkmire",
			["Reach"] = "The Reach",
			["Summerset"] = "Summerset",
			["Vvardenfell"] = "Vvardenfell",
			["WesternSkyrim"] = "Western Skyrim",
			["Wrothgar"] = "Wrothgar"
		};
		#endregion

		#region Constructors
		[JobInfo("One-Off Parse Job")]
		public OneOffParseJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string? LogDetails => this.EditSummary;

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Add zones to contraband and remove section markings";
		#endregion

		#region Protected Override Methods
		protected override void AfterLoadPages() => WikiStack.UnparsedTags.Remove("section");

		protected override void BeforeLoadPages() => WikiStack.UnparsedTags.Add("section");

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:ESO Contraband Item", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

		protected override void PageLoaded(Page page)
		{
			var old = page.Text;
			base.PageLoaded(page);
			if (!string.Equals(page.Text, old, StringComparison.Ordinal))
			{
				page.Text = Regex.Replace(page.Text, @"\s{2,}\{\{ESO Contraband Item", "\n{{ESO Contraband Item", RegexOptions.None, Globals.DefaultRegexTimeout);
				page.Text = Regex.Replace(page.Text, @"(\{\{ESO Contraband Item.*?\}\})\s+", "$1\n", RegexOptions.None, Globals.DefaultRegexTimeout);
			}
		}

		protected override void ParseText(ContextualParser parser)
		{
			var activeSections = new HashSet<string>(StringComparer.Ordinal);
			var unknownSections = new HashSet<string>(StringComparer.Ordinal);
			foreach (var node in parser)
			{
				if (node is ITagNode tagNode && tagNode.Attributes is not null && string.Equals(tagNode.Name, "section", StringComparison.Ordinal))
				{
					var attribMatch = AttribRegex.Match(tagNode.Attributes);
					var key = attribMatch.Groups["key"].Value;
					var values = attribMatch.Groups["value"].Value;
					var split = values.Split(TextArrays.Comma);
					foreach (var value in split)
					{
						var trimmed = value.Trim();
						if (!KnownTypes.Contains(trimmed))
						{
							if (Zones.ContainsKey(trimmed))
							{
								if (string.Equals(key, "begin", StringComparison.Ordinal))
								{
									activeSections.Add(Zones[trimmed]);
								}
								else
								{
									activeSections.Remove(Zones[trimmed]);
								}
							}
							else
							{
								unknownSections.Add(tagNode.Attributes);
							}
						}
					}
				}
				else if (node is SiteTemplateNode template && template.TitleValue is Title title && title.PageNameEquals("ESO Contraband Item"))
				{
					if (activeSections.Count > 0)
					{
						template.Add("zone", string.Join(',', activeSections));
					}
				}
			}

			if (activeSections.Count > 0)
			{
				Debug.WriteLine("Leftover on " + parser.Page.FullPageName);
			}

			parser.RemoveAll<ITagNode>(tag => string.Equals(tag.Name, "section", StringComparison.Ordinal));
			foreach (var section in unknownSections)
			{
				Debug.WriteLine($"Unknown: {section} ({parser.Page.FullPageName})");
			}
		}
		#endregion
	}
}
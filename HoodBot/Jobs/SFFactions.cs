namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFFactions : CreateOrUpdateJob<SFFactions.Redirect>
	{
		#region Constructors
		[JobInfo("Factions", "Starfield")]
		public SFFactions(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "faction";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create faction page";

		protected override bool IsValid(ContextualParser parser, Redirect item) => parser.Page.Text.StartsWith("#REDIRECT", StringComparison.Ordinal);

		protected override IDictionary<Title, Redirect> LoadItems()
		{
			var csv = new CsvFile();
			csv.Load(Starfield.Folder + "Factions.csv", true);
			var items = this.ParseFactions(csv);
			var entries = this.GetFactionEntries(csv);
			var first = entries.Keys.First() ?? " ";
			var letter = char.ToUpper(first[0], CultureInfo.CurrentCulture);
			var pageName = TitleFactory.FromUnvalidated(this.Site, "Starfield:Factions " + letter);
			var page = this.Site.CreatePage(pageName);
			foreach (var kvp in entries)
			{
				if (letter != char.ToUpper(kvp.Key[0], CultureInfo.CurrentCulture))
				{
					this.Pages.Add(page);
					letter = char.ToUpper(kvp.Key[0], CultureInfo.CurrentCulture);
					pageName = TitleFactory.FromUnvalidated(this.Site, "Starfield:Factions " + letter);
					page = this.Site.CreatePage(pageName);
				}

				page.Text += $"=={kvp.Key}==\n";
				var entryList = kvp.Value;
				entryList.Sort((x, y) => string.Compare(x.EditorId, y.EditorId, StringComparison.Ordinal));
				foreach (var entry in entryList)
				{
					if (entryList.Count > 1)
					{
						page.Text += $"==={entry.Header}===\n";
					}

					var sb = new StringBuilder();
					sb
						.Append("{{Factions\n")
						.Append($"|edid={entry.EditorId}\n")
						.Append($"|formid={entry.FormId}\n");
					if (entry.Members.Count > 0)
					{
						sb
							.Append("|members={{Factions/Members\n")
							.Append("  |members={{List|sep=,&#32;\n");
						entry.Members.Sort();
						foreach (var member in entry.Members)
						{
							sb
								.Append("    |[[")
								.Append(member.FullPageName())
								.Append("|]]\n");
						}

						sb.Append("}}}}");
					}

					sb.Append("}}\n\n");
					page.Text += sb.ToString();
				}
			}

			if (page.Text.Length > 0)
			{
				page.Text = "{{Faction Header}}\n";
				this.Pages.Add(page);
			}

			return items;
		}

		protected override string NewPageText(Title title, Redirect item)
		{
			var retval = $"#REDIRECT [[Starfield:Factions {item.Letter}#{item.Section}]] [[Category:Redirects to Broader Subjects]] [[Category:Starfield-Factions]]";
			var origName = (this.Disambiguator is not null && title.PageName.EndsWith(" (" + this.Disambiguator + ")", StringComparison.Ordinal))
				? title.PageName[..(this.Disambiguator.Length + 3)]
				: title.PageName;
			var data = string.Empty;
			if (item.AltName is not null)
			{
				data += "|altname=" + item.AltName;
			}

			if (origName.Contains('(', StringComparison.Ordinal))
			{
				data += "|catname=" + origName;
			}

			if (data.Length > 0)
			{
				retval += "\n{{Faction Data" + data + "}}";
			}

			return retval;
		}
		#endregion

		#region Private Classes
		private Dictionary<string, TitleCollection> GetNPCs()
		{
			var npcs = new TitleCollection(this.Site);
			npcs.GetCategoryMembers("Starfield-NPCs");
			var members = new Dictionary<string, TitleCollection>(StringComparer.Ordinal);
			var npcsFile = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			npcsFile.Load(Starfield.Folder + "Npcs.csv", true);
			{
				foreach (var row in npcsFile)
				{
					var name = row["Name"];
					if (name.Length > 0)
					{
						var factionText = row["Factions"];
						var factions = factionText.Length == 0
							? []
							: factionText.Split(TextArrays.Comma);
						var title =
							npcs.TryGetValue("Starfield:" + name + " (NPC)", out var page) ? page :
							npcs.TryGetValue("Starfield:" + name, out page) ? page :
							throw new KeyNotFoundException();
						foreach (var faction in factions)
						{
							var memberList = members.TryGetValue(faction, out var list) ? list : new TitleCollection(this.Site);
							memberList.TryAdd(title);
							members[faction] = memberList;
						}
					}
				}
			}

			return members;
		}

		private Dictionary<Title, Redirect> ParseFactions(CsvFile csv)
		{
			var items = new Dictionary<Title, Redirect>();
			foreach (var row in csv)
			{
				var name = row["Name"]
					.Replace("[", string.Empty, StringComparison.Ordinal)
					.Replace("]", string.Empty, StringComparison.Ordinal);
				var edid = row["EditorID"];
				var prefName = name.Length > 0 ? name : edid;
				var altName = name.Length > 0 && !string.Equals(name, edid, StringComparison.Ordinal) ? name : null;
				if (name.Length > 0)
				{
					items.TryAdd(TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name), new Redirect(name[0], name, null));
				}

				items.TryAdd(TitleFactory.FromUnvalidated(this.Site, "Starfield:" + edid), new Redirect(prefName[0], edid, altName));
			}

			return items;
		}

		private SortedDictionary<string, List<Entry>> GetFactionEntries(CsvFile csv)
		{
			var npcs = this.GetNPCs();
			var entries = new SortedDictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);
			foreach (var row in csv)
			{
				var name = row["Name"]
					.Replace("[", string.Empty, StringComparison.Ordinal)
					.Replace("]", string.Empty, StringComparison.Ordinal);
				var edid = row["EditorID"];
				var factionMembers = npcs.TryGetValue(edid, out var membs) ? membs : new TitleCollection(this.Site);
				var entry = new Entry(edid, row["FormID"][2..], edid, factionMembers);
				var prefName = name.Length > 0 ? name : edid;
				var entryList = entries.TryGetValue(prefName, out var list) ? list : [];
				entryList.Add(entry);
				entries[prefName] = entryList;
			}

			return entries;
		}

		#endregion

		#region Internal Classes
		internal sealed record Entry(string Header, string FormId, string EditorId, TitleCollection Members);

		internal sealed record Redirect(char Letter, string Section, string? AltName);
		#endregion
	}
}
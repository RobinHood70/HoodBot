namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFMissions : CreateOrUpdateJob<SFMissions.Mission>
	{
		#region Constructors
		[JobInfo("Missions", "Starfield")]
		public SFMissions(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "mission";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create/update mission";

		protected override bool IsValid(ContextualParser parser, Mission item) => parser.FindSiteTemplate("Mission Header") is not null;

		protected override IDictionary<Title, Mission> LoadItems()
		{
			var items = new Dictionary<Title, Mission>();
			var disambigs = this.ParseDisambigs();
			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			csv.Load(LocalConfig.BotDataSubPath("Starfield/QuestStages.csv"), true);
			foreach (var row in csv)
			{
				var name = row["QuestFull"];
				var edid = row["QuestEditorID"];
				name = SanitizeName(name);
				var baseName = name;
				if (
					name.Length > 0 &&
					!name.Contains('_', StringComparison.Ordinal) &&
					!name.Contains("convers", StringComparison.OrdinalIgnoreCase) &&
					!name.Contains("dialog", StringComparison.OrdinalIgnoreCase) &&
					!edid.Contains("dialog", StringComparison.OrdinalIgnoreCase))
				{
					if (disambigs.TryGetValue(edid, out var disambig))
					{
						name += " (" + disambig + ")";
					}

					var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
					if (!items.TryGetValue(title, out var mission))
					{
						mission = new Mission(edid, baseName, disambig, []);
						items.Add(title, mission);
					}

					mission.Stages.Add(new Stage(row["INDX1"], row["NAM2"].Trim(), row["CNAM"].Trim()));
				}
			}

			return items;
		}

		protected override string NewPageText(Title title, Mission item)
		{
			var sb = new StringBuilder();
			var name = item.Name;
			sb
				.Append("{{NewLine}}\n")
				.Append("{{Mission Header\n");
			if (!string.Equals(title.LabelName(), name, StringComparison.Ordinal))
			{
				sb.Append($"|title={name}\n");
			}

			sb
				.Append("|type=\n")
				.Append("|Giver=\n")
				.Append("|Icon=\n")
				.Append("|Reward={{Huh}}\n")
				.Append($"|ID={item.EditorId}\n")
				.Append("|Prev=\n")
				.Append("|Next=\n")
				.Append("|Loc=\n")
				.Append("|image=\n")
				.Append("|imgdesc=\n")
				.Append("|description=\n")
				.Append("}}\n");

			return sb.ToString()[12..] + "\n{{Stub|Mission}}";
		}

		protected override void PageLoaded(ContextualParser parser, Mission item)
		{
			var sb = new StringBuilder();
			sb
				.Append("\n\n== Mission Stages ==\n")
				.Append("{{Mission Entries\n");
			var missing = new List<string>();
			foreach (var stage in item.Stages)
			{
				if (stage.Entry.Length == 0 && stage.Comment.Length == 0)
				{
					missing.Add(stage.Index);
				}
				else
				{
					sb
						.Append('|')
						.Append(stage.Index)
						.Append('|')
						.Append('|');
					if (stage.Comment.Length > 0)
					{
						sb
							.Append("{{Mission Comment|")
							.Append(stage.Comment.Replace("=", "{{=}}", StringComparison.Ordinal))
							.Append("}}");
						if (stage.Entry.Length > 0)
						{
							sb.Append("<br>");
						}
					}

					sb
						.Append(stage.Entry)
						.Append('\n');
				}
			}

			if (missing.Count > 0)
			{
				sb
					.Append("|missing=")
					.AppendJoin(", ", missing)
					.Append('\n');
				if (missing.Count == item.Stages.Count)
				{
					sb.Append("|allmissing=1\n");
				}
			}

			sb.Append("}}");

			var insertLoc = parser.FindIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Stub"));
			if (insertLoc == -1)
			{
				insertLoc = parser.Count;
			}

			parser.Insert(insertLoc, parser.Factory.TextNode(sb.ToString()));
		}
		#endregion

		#region Private Static Methods
		private static string SanitizeName(string name)
		{
			if (name.StartsWith('[') && name.EndsWith(']'))
			{
				name = name[1..^1];
			}

			name = name
				.Replace("<Alias=", string.Empty, StringComparison.Ordinal)
				.Replace(">", string.Empty, StringComparison.Ordinal)
				.Replace('[', '(')
				.Replace(']', ')');
			return name;
		}
		#endregion

		#region Private Methods
		private Dictionary<string, string> ParseDisambigs()
		{
			var disambigLines = File.ReadAllLines(LocalConfig.BotDataSubPath("Starfield/Quests Disambigs.txt"));
			var disambigs = new Dictionary<string, string>(StringComparer.Ordinal);
			var disambigPages = new PageCollection(this.Site);
			foreach (var disambig in disambigLines)
			{
				var split = disambig.Split(TextArrays.Tab);
				disambigs.Add(split[0], split[2]);
				var name = SanitizeName(split[1]);
				var pageName = "Starfield:" + name;
				if (!disambigPages.TryGetValue(pageName, out var disambigPage))
				{
					disambigPage = this.Site.CreatePage(pageName);
					disambigPage.Text = $"'''{name}''' may refer to:\n";
					disambigPages.Add(disambigPage);
				}

				var linkText = SiteLink.FromText(this.Site, pageName + " (" + split[2] + ")").ToString();
				disambigPage.Text += "* " + linkText + "\n";
			}

			var disambigCheck = new PageCollection(this.Site);
			disambigCheck.GetTitles(disambigPages.ToFullPageNames());
			foreach (var page in disambigPages)
			{
				var checkPage = disambigCheck[page.Title];
				if (!string.Equals(checkPage.Revisions[0].User, "RobinHood70", StringComparison.Ordinal))
				{
					Debug.WriteLine(page.Title.FullPageName() + " modified.");
				}

				page.Text += "\n{{Disambig}}";
				page.Save("Replace with disambiguation page", false);
			}

			return disambigs;
		}
		#endregion

		#region Internal Records
		internal sealed record Mission(string EditorId, string Name, string? Disambig, List<Stage> Stages);

		internal sealed record Stage(string Index, string Comment, string Entry);
		#endregion
	}
}
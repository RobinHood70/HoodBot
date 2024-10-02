namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SFAffinities : EditJob
	{
		#region Fields
		private readonly Dictionary<string, List<Affinity>> affinities = new(StringComparer.Ordinal);
		private Page? affinityPage;
		#endregion

		#region Constructors
		[JobInfo("Affinities", "Starfield")]
		public SFAffinities(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Starfield Affinities";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Add affinities";

		protected override void LoadPages()
		{
			var fileName = Starfield.ModFolder + "Affinities_1.9.51.csv";
			var csv = new CsvFile(fileName)
			{
				// Field separator may be different in header from text. Just fix it manually.
				Encoding = Encoding.GetEncoding(1252),
				FieldDelimiter = '\0',
				HasHeader = true
			};

			csv.Load();
			foreach (var row in csv)
			{
				var mission = row["Mission"];
				if (!this.affinities.TryGetValue(mission, out var list))
				{
					list = [];
					this.affinities[mission] = list;
				}

				var affinity = new Affinity(
					 row["Name"],
					 row["Context"],
					 SplitReaction(row["Andreja"]),
					 SplitReaction(row["Barrett"]),
					 SplitReaction(row["SamCoe"]),
					 SplitReaction(row["SarahMorgan"]));
				if (!IsIndifferent(affinity.Andreja) ||
					!IsIndifferent(affinity.Barrett) ||
					!IsIndifferent(affinity.SamCoe) ||
					!IsIndifferent(affinity.SarahMorgan))
				{
					list.Add(affinity);
				}
			}

			this.affinityPage = this.Site.LoadPage("Starfield:Affinity") ?? throw new InvalidOperationException();
			var titles = new TitleCollection(this.Site);
			RemoveEmpties(this.affinities);
			this.Load(this.affinities, titles);
			this.Pages.Add(this.affinityPage);
		}

		protected override void PageLoaded(Page page)
		{
			var newPage = page.Exists
				? page
				: this.affinityPage ?? throw new InvalidOperationException();
			var parser = new ContextualParser(newPage);
			var sections = parser.ToSections(2);
			var walkthroughNum = -1;
			for (var sectionNum = 0; sectionNum < sections.Count; sectionNum++)
			{
				var section = sections[sectionNum];
				var headerTitle = section.Header?.GetTitle(true);
				Debug.WriteLine($"{newPage.Title.FullPageName()}: '{headerTitle}'");
				if (page.Exists && string.Equals(headerTitle, "Companion Affinity", StringComparison.Ordinal))
				{
					walkthroughNum = sectionNum + 1;
					break;
				}
			}

			if (walkthroughNum == -1)
			{
				for (var sectionNum = 0; sectionNum < sections.Count; sectionNum++)
				{
					var section = sections[sectionNum];
					var headerTitle = section.Header?.GetTitle(true);
					if (string.Equals(headerTitle, "Walkthrough", StringComparison.Ordinal))
					{
						walkthroughNum = sectionNum + 1;
						break;
					}

					if (string.Equals(headerTitle, "Detailed Walkthrough", StringComparison.Ordinal))
					{
						walkthroughNum = sectionNum + 1;
						break;
					}

					if (string.Equals(headerTitle, "Mission Stages", StringComparison.Ordinal))
					{
						walkthroughNum = sectionNum;
						break;
					}
				}
			}

			if (walkthroughNum == -1)
			{
				this.Warn($"No good section locations on {newPage.Title.FullPageName()}");
				walkthroughNum = sections.Count;
			}

			var affinityList = this.affinities[page.Title.PageName];
			var text = BuildAffinities(affinityList);
			if (!page.Exists && page.Title.PageName.Length > 0)
			{
				text = $"({page.Title.PageName})<br>" + text;
			}

			var affinitiesSection = Section.FromText(parser.Factory, "Companion Affinity", text);
			sections.Insert(walkthroughNum, affinitiesSection);
			parser.FromSections(sections);
			parser.UpdatePage();
		}
		#endregion

		#region Private Static Methods
		private static string BuildAffinities(List<Affinity> affinityList)
		{
			var sb = new StringBuilder();
			var intro = affinityList.Count == 1
				? "There is one [[Starfield:Affinity|affinity]] event"
				: "There are multiple [[Starfield:Affinity|affinity]] events";
			sb
				.Append(intro)
				.Append(" that can happen during this mission.\n\n")
				.Append("{{Affinity Table|\n");
			foreach (var affinity in affinityList)
			{
				sb
					.Append(affinity.Context)
					.Append("<ref group=affinity>")
					.Append(affinity.Context)
					.Append("</ref> | ")
					.Append(affinity.Andreja)
					.Append(" | ")
					.Append(affinity.Barrett)
					.Append(" | ")
					.Append(affinity.SamCoe)
					.Append(" | ")
					.Append(affinity.SarahMorgan)
					.Append(" |\n");
			}

			sb
				.Append("}}\n")
				.Append("<references group=affinity/>\n\n");

			return sb.ToString();
		}

		private static bool IsIndifferent(string reaction) =>
			reaction.Length == 0 ||
			string.Equals(reaction, "indifferent", StringComparison.Ordinal);

		private static void RemoveEmpties(Dictionary<string, List<Affinity>> affinities)
		{
			var remove = new List<string>();
			foreach (var kvp in affinities)
			{
				if (kvp.Value.Count == 0)
				{
					remove.Add(kvp.Key);
				}
			}

			foreach (var affinity in remove)
			{
				affinities.Remove(affinity);
			}
		}

		private static string SplitReaction(string reaction)
		{
			var split = reaction.Split('_');
			var retval = split[^1].TrimEnd();
			if (string.Equals(retval, "Neutral", StringComparison.Ordinal))
			{
				retval = "WantsToTalk";
			}

			return retval.ToLowerInvariant();
		}
		#endregion

		#region Private Methods
		private void Load(Dictionary<string, List<Affinity>> affinities, TitleCollection titles)
		{
			foreach (var kvp in affinities)
			{
				titles.Add("Starfield:" + kvp.Key);
			}

			this.Pages.GetTitles(titles);
		}

		#endregion

		#region Private Records
		private sealed record Affinity(string Name, string Context, string Andreja, string Barrett, string SamCoe, string SarahMorgan);
		#endregion
	}
}
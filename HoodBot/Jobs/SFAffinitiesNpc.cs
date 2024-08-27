namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;

	[method: JobInfo("Affinities (NPC)", "Starfield")]
	internal sealed class SFAffinitiesNpc(JobManager jobManager) : EditJob(jobManager)
	{
		#region Fields
		private readonly SortedDictionary<string, List<NpcAffinity>> andreja = new(StringComparer.Ordinal);
		private readonly SortedDictionary<string, List<NpcAffinity>> barrett = new(StringComparer.Ordinal);
		private readonly SortedDictionary<string, List<NpcAffinity>> samcoe = new(StringComparer.Ordinal);
		private readonly SortedDictionary<string, List<NpcAffinity>> sarahmorgan = new(StringComparer.Ordinal);
		#endregion

		#region Public Override Properties
		public override string LogName => "Starfield Affinities";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Add affinities";

		protected override void LoadPages()
		{
			var fileName = LocalConfig.BotDataSubPath(@"Starfield\Affinities_1.9.51.csv");
			var file = new CsvFile
			{
				FieldDelimiter = '\0',
				FieldSeparator = ';'
			};

			file.Load(fileName, true);
			foreach (var row in file)
			{
				this.GetNpcAffinity(row, "Andreja", this.andreja);
				this.GetNpcAffinity(row, "Barrett", this.barrett);
				this.GetNpcAffinity(row, "SamCoe", this.samcoe);
				this.GetNpcAffinity(row, "SarahMorgan", this.sarahmorgan);
			}

			this.Pages.GetTitles("Starfield:Andreja", "Starfield:Barrett", "Starfield:Sam Coe", "Starfield:Sarah Morgan");
		}

		protected override void PageLoaded(Page page)
		{
			var dict = page.Title.PageName switch
			{
				"Andreja" => this.andreja,
				"Barrett" => this.barrett,
				"Sam Coe" => this.samcoe,
				"Sarah Morgan" => this.sarahmorgan,
				_ => throw new InvalidOperationException()
			};

			var sb = new StringBuilder();
			foreach (var (mission, list) in dict)
			{
				sb
					.Append("\n===")
					.Append(mission)
					.Append("===\n")
					.Append("{|class=\"wikitable\"\n")
					.Append("! Event !! Affinity Reaction");
				foreach (var item in list)
				{
					sb
						.Append('\n')
						.Append("|-\n")
						.Append(this.Site.Culture, $"| {item.Context} || {item.Reaction}<br>")
						.Append(this.Site.Culture, $"{item.ReactionValue:+#;-#;0}");
				}

				sb.Append("\n|}");
			}

			page.Text += "\n\n==Reactions==" + sb.ToString();
		}
		#endregion

		#region Private Methods

		private void GetNpcAffinity(CsvRow row, string name, IDictionary<string, List<NpcAffinity>> dict)
		{
			var mission = row["Mission"];
			if (mission.Length == 0)
			{
				return;
			}

			var context = row["Context"];
			string reaction = row[name];
			if (reaction.Length > 0)
			{
				if (!dict.TryGetValue(mission, out var list))
				{
					list = [];
					dict[mission] = list;
				}

				var reactionType = this.SplitReaction(reaction);
				var dvalue = double.Parse(row[name + "Value"].Replace(',', '.'), this.Site.Culture);
				var value = (int)dvalue;
				if (dvalue - value != 0)
				{
					throw new InvalidOperationException();
				}

				list.Add(new NpcAffinity(context, reactionType, value));
			}
		}

		private string SplitReaction(string reaction)
		{
			var split = reaction.Split('_');
			var retval = split[^1].TrimEnd();
			if (string.Equals(retval, "Neutral", StringComparison.Ordinal))
			{
				retval = "WantsToTalk";
			}

			return retval.UpperFirst(this.Site.Culture);
		}
		#endregion

		#region Private Records
		private sealed record NpcAffinity(string Context, string Reaction, int ReactionValue);
		#endregion
	}
}
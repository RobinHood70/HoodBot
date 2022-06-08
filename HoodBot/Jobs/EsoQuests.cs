namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class EsoQuests : EditJob
	{
		#region Constants
		private const string QuestQuery =
		"SELECT\n" +
			"quest.id,\n" +
			"quest.internalId,\n" +
			"quest.name,\n" +
			"quest.type,\n" +
			"quest.repeatType,\n" +
			"quest.backgroundText,\n" +
			"quest.objective,\n" +
			"quest.zone,\n" +
			"location.zone locZone\n" +
		"FROM\n" +
			"quest INNER JOIN\n" +
			"location ON quest.locationId = location.id\n" +
		"WHERE\n" +
			"quest.internalId != -1\n" +
		"ORDER BY quest.id";

		private const string StageQuery =
		"SELECT\n" +
		   "questStep.id,\n" +
		   "questStep.questId,\n" +
		   "questStep.text,\n" +
		   "questStep.visibility,\n" +
		   "location.zone zone\n" +
	   "FROM\n" +
		   "questStep INNER JOIN\n" +
		   "location ON questStep.locationId = location.id\n" +
	   "WHERE questStep.questId IN (<questIds>)\n" +
	   "ORDER BY questStep.questId, questStep.stageIndex, questStep.stepIndex";

		private const string ConditionQuery =
		"SELECT questId, questStepId, text, isFail, isComplete\n" +
		"FROM uesp_esolog.questCondition\n" +
		"WHERE questId IN (<questIds>)\n" +
		"ORDER BY questId, stepIndex, conditionIndex";

		private const string RewardsQuery =
		"SELECT questId, name, itemId, collectId, quantity, quality, type\n" +
		"FROM uesp_esolog.questReward\n" +
		"WHERE questId IN (<questIds>);";

		private const string TemplateName = "Online Quest Header";
		#endregion

		#region Static Fields
		private static readonly Dictionary<long, string> PageNameOverrides = new()
		{
			[6780] = "The Long Game (High Isle)"
		};

		private static readonly Dictionary<int, string> QuestTypes = new()
		{
			[0] = string.Empty, // "None"
			[1] = "Group",
			[2] = "Main",
			[3] = "Guild",
			[4] = "Crafting",
			[5] = "Dungeon",
			[6] = "Raid",
			[7] = "PVP",
			[8] = "Class",
			[9] = "QA Test",
			[10] = "PVP Group",
			[11] = "PVP Grand",
			[12] = "Holiday Event",
			[13] = "Battleground",
			[14] = "Prologue",
			[15] = "Pledge",
			[16] = "Companion",
		};

		private static readonly Dictionary<int, string> RepeatTypes = new()
		{
			[0] = string.Empty,
			[1] = "Immediately",
			[2] = "Daily",
			[3] = "Once per event"
		};

		private static readonly Dictionary<Visibility, string> Visibilities = new()
		{
			[Visibility.Normal] = string.Empty,
			[Visibility.Hint] = "hint",
			[Visibility.Optional] = "optional",
			[Visibility.Hidden] = "hidden",
		};
		#endregion

		#region Fields
		private readonly Dictionary<Title, QuestData> quests = new(SimpleTitleComparer.Instance);
		private readonly Dictionary<long, Title> idToTitle = new();
		private readonly TitleCollection allTitles;
		#endregion

		#region Constructors
		[JobInfo("Update Quests", "ESO Update")]
		public EsoQuests(JobManager jobManager)
			: base(jobManager)
		{
			this.allTitles = new TitleCollection(this.Site);
		}
		#endregion

		#region Private Enumerations
		private enum Visibility
		{
			Normal = -1,
			Hint,
			Optional,
			Hidden,
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Create Missing ESO Quests";
		#endregion

		#region Protected Override Properties
		protected override Action<EditJob, Page>? EditConflictAction => null;

		protected override string EditSummary => this.LogName;
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			this.StatusWriteLine("Getting ESO titles");
			this.allTitles.GetNamespace(UespNamespaces.Online);
		}

		protected override void LoadPages()
		{
			var titles = new PageCollection(this.Site, new PageLoadOptions(PageModules.Info, true));
			titles.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);
			foreach (var quest in Database.RunQuery(EsoLog.Connection, QuestQuery, 100000, row => new QuestData(row)))
			{
				var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], quest.Name);
				var titleDisambig = TitleFactory.FromValidated(this.Site[UespNamespaces.Online], title.PageName + " (quest)");
				if (!titles.Contains(title) &&
					!titles.Contains(titleDisambig) &&
					this.allTitles.TryGetValue(title, out var finalTitle))
				{
					finalTitle = this.allTitles.Contains(titleDisambig)
						? throw new InvalidOperationException("Could not find valid page to add quest information to.")
						: titleDisambig;
					this.quests.Add(finalTitle, quest);
				}
			}

			if (this.quests.Count > 0)
			{
				var sb = new StringBuilder();
				foreach (var quest in this.quests)
				{
					sb
						.Append(',')
						.Append(quest.Value.Id);
				}

				if (sb.Length > 0)
				{
					sb.Remove(0, 1);
					var whereText = sb.ToString();
					this.GetStages(whereText);
					this.GetConditions(whereText);
					this.GetRewards(whereText);
					this.GetPlaces();
				}
			}

			foreach (var quest in this.quests)
			{
				var page = this.Site.CreatePage(quest.Key, this.NewPageText(quest.Value));
				this.Pages.Add(page);
			}
		}
		#endregion

		#region Private Static Methods
		private static List<string> GetJournalEntries(Dictionary<string, List<Condition>> mergedStages)
		{
			List<string> journalEntries = new();
			foreach (var kvp in mergedStages)
			{
				var split = kvp.Key.Split(TextArrays.At);
				if (split[0].Length > 2)
				{
					journalEntries.Add(split[0]);
				}

				journalEntries.AddRange(QuestObjectives(split[1], kvp.Value));
			}

			return journalEntries;
		}

		private static List<string> QuestObjectives(string objectiveType, List<Condition> conditions)
		{
			List<string> retval = new();
			foreach (var condition in conditions)
			{
				if (condition.Text.Length > 0 && !string.Equals(condition.Text, "TRACKER GOAL TEXT", StringComparison.Ordinal))
				{
					var conditionText = condition.Text.TrimEnd(TextArrays.Colon);
					var fullText = $"{{{{Online Quest Objective|{objectiveType}|{conditionText}}}}}";
					if (!retval.Contains(fullText, StringComparer.Ordinal))
					{
						retval.Add(fullText);
					}
				}
			}

			return retval;
		}
		#endregion

		#region Private Methods
		private void GetConditions(string whereText)
		{
			this.StatusWriteLine("Getting condition data");
			var conditions = Database.RunQuery(EsoLog.Connection, ConditionQuery.Replace("<questIds>", whereText, StringComparison.Ordinal), row => new Condition(row));
			foreach (var condition in conditions)
			{
				var quest = this.QuestFromId(condition.QuestId);
				if (quest.Stages.Find(item => item.Id == condition.StageId) is Stage stage)
				{
					stage.Conditions.Add(condition);
				}
			}
		}

		private void GetPlaces()
		{
			var places = EsoSpace.GetPlaces(this.Site);
			foreach (var quest in this.quests)
			{
				if (!string.IsNullOrEmpty(quest.Value.Zone))
				{
					if (places[quest.Value.Zone] is Place place)
					{
						while (!string.Equals(place.TypeText, "Zone", StringComparison.Ordinal) && place.Zone != null && places[place.Zone] is Place newZone)
						{
							place = newZone;
						}

						quest.Value.Zone = place.TitleName;
					}
				}
			}
		}

		private void GetRewards(string whereText)
		{
			this.StatusWriteLine("Getting rewards data");
			var rewardData = Database.RunQuery(EsoLog.Connection, RewardsQuery.Replace("<questIds>", whereText, StringComparison.Ordinal), row => new Reward(row));
			var rewards = new List<Reward>();
			var previousRewards = new HashSet<long>();
			foreach (var reward in rewardData)
			{
				var quest = this.QuestFromId(reward.QuestId);
				if (!previousRewards.Add(reward.ItemId))
				{
					quest.AddReward(reward);
				}
			}
		}

		private void GetStages(string whereText)
		{
			this.StatusWriteLine("Getting stage data");
			var stages = Database.RunQuery(EsoLog.Connection, StageQuery.Replace("<questIds>", whereText, StringComparison.Ordinal), row => new Stage(row));
			foreach (var stage in stages)
			{
				var quest = this.QuestFromId(stage.QuestId);
				quest.Stages.Add(stage);
			}
		}

		private QuestData QuestFromId(long id) => this.quests[this.idToTitle[id]];

		private Dictionary<string, List<Condition>> MergeStages(QuestData quest, SortedSet<string> locs)
		{
			Dictionary<string, List<Condition>> mergedStages = new(StringComparer.Ordinal);
			foreach (var stage in quest.Stages)
			{
				if (!string.Equals(stage.Zone, "Tamriel", StringComparison.Ordinal) && !string.Equals(stage.Zone, quest.Zone, StringComparison.Ordinal))
				{
					Title title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], stage.Zone);
					locs.Add(title.AsLink(LinkFormat.LabelName));
				}

				var finishText = stage.FinishText;
				var stageText = '|' + finishText + '|' + stage.Text + '@' + Visibilities[stage.Visibility];
				if (!mergedStages.TryGetValue(stageText, out var list))
				{
					list = new List<Condition>();
					mergedStages.Add(stageText, list);
				}

				foreach (var condition in stage.Conditions)
				{
					if (!list.Contains(condition))
					{
						list.Add(condition);
					}
				}
			}

			return mergedStages;
		}

		private string NewPageText(QuestData quest)
		{
			SortedSet<string> locs = new(StringComparer.Ordinal);
			var mergedStages = this.MergeStages(quest, locs);
			var journalEntries = GetJournalEntries(mergedStages);

			var sb = new StringBuilder()
				.AppendLine("{{Minimal|quest}}{{ONQP Header")
				.AppendLine("|summaryWritten=")
				.AppendLine("|summaryChecked=")
				.AppendLine("|walkthroughWritten=")
				.AppendLine("|walkthroughChecked=")
				.AppendLine("|stagesWritten=")
				.AppendLine("|stagesChecked=")
				.Append("}}");
			if (quest.Mod != null)
			{
				sb
					.Append("{{Mod Header|")
					.Append(quest.Mod)
					.Append("}}");
			}

			var questTypeText = QuestTypes[quest.Type]; // Split out because this can cause an error when a new type is introduced.
			sb
				.Append("{{")
				.AppendLine(TemplateName)
				.Append("|ID=").AppendLine(quest.InternalId >= 0 ? quest.InternalId.ToStringInvariant() : string.Empty)
				.Append("|type=").AppendLine(questTypeText)
				.AppendLine("|image=")
				.AppendLine("|imgdesc=")
				.AppendLine("|description=")
				.Append("|Zone=").AppendLine(quest.Zone)
				.AppendLine("|Faction=")
				.Append("|Obj=").AppendLine(quest.Objective)
				.AppendLine("|Giver=")
				.Append("|Loc=").AppendJoin(", ", locs).AppendLine()
				.AppendLine("|Prereq=")
				.AppendLine("|Prev=")
				.AppendLine("|Next=")
				.AppendLine("|Conc=")
				.Append("|Reward=").AppendLine(quest.GetRewardText())
				.Append("|XP=").AppendLine(quest.XP)
				.AppendLine("|Level=")
				.Append("|Journal=").AppendLine(quest.BackgroundText);
			if (quest.RepeatType > 0)
			{
				sb.Append("|Repeatable=").AppendLine(RepeatTypes[quest.RepeatType]);
			}

			sb
				.AppendLine("}}")
				.AppendLine()
				.AppendLine("==Quick Walkthrough==")
				.AppendLine("<!-- Instructions: Provide a point-by-point list of the key tasks that need to be completed for this quest. Spoilers should be avoided in the quick walkthrough. -->")
				.AppendLine()
				.AppendLine("==Detailed Walkthrough==")
				.AppendLine("<!-- Instructions: The detailed walkthrough should provide full information about the quest, organized into paragraphs. Spoilers belong in this section.--><!--")
				.AppendLine()
				.AppendLine("==Notes==")
				.AppendLine("Instructions: Add any miscellaneous notes about the quest here.--><!--")
				.AppendLine()
				.AppendLine("==Bugs==")
				.AppendLine("Instructions: Add any bugs using the following format:")
				.AppendLine("{{Bug|Bug description}}")
				.AppendLine("** Workaround")
				.AppendLine("-->")
				.AppendLine()
				.AppendLine("==Quest Stages==");
			if (journalEntries.Count > 0)
			{
				sb.AppendLine("{{Online Journal Entries");
				foreach (var entry in journalEntries)
				{
					sb.AppendLine(entry);
				}

				sb.AppendLine("}}");
			}

			sb
				.AppendLine("{{Online Quest Stages Notes}}")
				.AppendLine()
				.AppendLine("{{Stub|Quest}}");

			return sb.ToString();
		}
		#endregion

		#region Private Classes
		private sealed class Condition : IEquatable<Condition>
		{
			#region Constructors
			public Condition(IDataRecord row)
			{
				this.IsComplete = (sbyte)row["isComplete"] == 1;
				this.IsFail = (sbyte)row["isFail"] == 1;
				this.QuestId = (long)row["questId"];
				this.StageId = (long)row["questStepId"];
				this.Text = (string)row["text"];
			}
			#endregion

			#region Public Properties
			public bool IsComplete { get; }

			public bool IsFail { get; }

			public long QuestId { get; }

			public long StageId { get; }

			public string Text { get; }

			public bool Equals(Condition? other) =>
				other != null &&
				this.IsComplete == other.IsComplete &&
				this.IsFail == other.IsFail &&
				string.Equals(this.Text, other.Text, StringComparison.Ordinal);
			#endregion

			#region Public Override Methods
			public override bool Equals(object? obj) => this.Equals(obj as Condition);

			public override int GetHashCode() => HashCode.Combine(this.IsComplete, this.IsFail, this.Text);
			#endregion
		}

		private sealed class QuestData
		{
			#region Fields
			private readonly List<Reward> rewards = new();
			#endregion

			#region Constructors
			public QuestData(IDataRecord row)
			{
				var fromEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252) ?? throw new InvalidOperationException();
				var toEncoding = Encoding.UTF8;
				var bgText = (string)row["backgroundText"];
				var bgBytes = fromEncoding.GetBytes(bgText);
				this.BackgroundText = toEncoding.GetString(bgBytes); // Fix UTF8 stored as CP-1252
				this.Id = (long)row["id"];
				this.InternalId = (int)row["internalId"];
				if (!PageNameOverrides.TryGetValue(this.InternalId, out var name))
				{
					name = (string)row["name"];
				}

				name = name.Replace("  ", " ", StringComparison.Ordinal); // Handles "Capture  Farm"
				var offset = name.IndexOf('\n', StringComparison.Ordinal);
				if (offset != -1)
				{
					name = name[0..offset];
				}

				offset = name.LastIndexOf(" (", StringComparison.Ordinal);
				if (offset != -1)
				{
					name = name[0..offset];
				}

				this.Name = name;
				this.Objective = (string)row["objective"];
				this.RepeatType = (short)row["repeatType"];
				this.Type = (short)row["type"];
				var zone = (string)row["zone"];
				if (zone.Length == 0 || string.Equals(zone, "Tamriel", StringComparison.Ordinal))
				{
					zone = (string)row["locZone"];
					if (string.Equals(zone, "Tamriel", StringComparison.Ordinal))
					{
						zone = string.Empty;
					}
				}

				this.Zone = zone;
			}
			#endregion

			#region Public Properties
			public string BackgroundText { get; }

			public long Id { get; }

			public int InternalId { get; }

			public string? Mod => this.Zone switch
			{
				"Southern Elsweyr" => "Dragonhold",
				_ => null,
			};

			public string Name { get; }

			public string Objective { get; }

			public int RepeatType { get; }

			public List<Stage> Stages { get; } = new List<Stage>();

			public int Type { get; }

			public string? XP { get; private set; }

			public string Zone { get; set; }
			#endregion

			#region Public Methods
			public void AddReward(Reward reward) => this.rewards.Add(reward);

			public string GetRewardText()
			{
				List<string> rewardList = new();
				foreach (var reward in this.rewards)
				{
					switch (reward.RewardType)
					{
						case -1:
							this.XP = $"{{{{ESO XP|{reward.Quantity}}}}}";
							break;
						case 1:
							rewardList.Add($"{{{{ESO Gold|{reward.Quantity}}}}} Gold");
							break;
						default:
							var rewardText = reward.Quantity > 1 ? reward.Quantity.ToStringInvariant() + ' ' : string.Empty;
							if (reward.ItemId == 0 && reward.CollectId == 0)
							{
								rewardText += reward.Name;
							}
							else
							{
								rewardText += "{{Item Link|" + reward.Name;
								if (reward.ItemId != 0)
								{
									rewardText += "|id=" + reward.ItemId.ToStringInvariant();
								}

								if (reward.CollectId != 0)
								{
									rewardText += "|collectid=" + reward.CollectId.ToStringInvariant();
								}

								if (reward.Quality > -1)
								{
									rewardText += "|quality=" + reward.Quality.ToStringInvariant();
								}

								rewardText += "|summary=1}}";
							}

							rewardList.Add(rewardText);
							break;
					}
				}

				return string.Join("<br>", rewardList);
			}

			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion
		}

		private sealed class Reward
		{
			public Reward(IDataRecord row)
			{
				this.CollectId = (int)row["collectId"];
				this.ItemId = (int)row["itemId"];
				this.RewardType = (short)row["type"];
				this.Name = (string)row["name"];
				this.Quality = (sbyte)row["quality"];
				this.Quantity = (int)row["quantity"];
				this.QuestId = (long)row["questId"];
			}

			public int CollectId { get; }

			public int ItemId { get; }

			public int RewardType { get; }

			public string Name { get; }

			public int Quality { get; }

			public int Quantity { get; }

			public long QuestId { get; }
		}

		private sealed class Stage
		{
			#region Constructors
			public Stage(IDataRecord row)
			{
				this.Id = (long)row["id"];
				this.Text = (string)row["text"];
				this.Visibility = (Visibility)(sbyte)row["visibility"];
				this.Zone = (string)row["zone"];
				this.QuestId = (long)row["questId"];
			}
			#endregion

			#region Public Properties
			public List<Condition> Conditions { get; } = new List<Condition>();

			public string FinishText
			{
				get
				{
					foreach (var condition in this.Conditions)
					{
						// Failure with no text seems to indicate possibility of failure of previous condition, but not guaranteed, so we ignore that.
						if (condition.IsFail && condition.Text.Length > 0)
						{
							return "fail";
						}

						if (condition.IsComplete)
						{
							return "fin";
						}
					}

					return string.Empty;
				}
			}

			public long Id { get; }

			public long QuestId { get; }

			public string Text { get; }

			public Visibility Visibility { get; }

			public string Zone { get; }
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Text;
			#endregion
		}
		#endregion
	}
}
namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class EsoQuests : EditJob
	{
		#region Constants
		private const string QuestQuery =
			@"SELECT 
				quest.id,
				quest.internalId,
				quest.name,
				quest.type,
				quest.repeatType,
				quest.backgroundText,
				quest.objective,
				quest.zone,
				location.zone locZone
			FROM
				quest INNER JOIN
				location ON quest.locationId = location.id";

		private const string StageQuery =
			@"SELECT 
				questStep.id,
				questStep.questId,
				questStep.text,
				questStep.visibility,
				location.zone zone
			FROM
				questStep INNER JOIN
				location ON questStep.locationId = location.id
			WHERE questStep.questId IN (<questIds>)
			ORDER BY questStep.questId, questStep.stageIndex, questStep.stepIndex";

		private const string ConditionQuery =
			@"SELECT questId, questStepId, text, isFail, isComplete
			FROM uesp_esolog.questCondition
			WHERE questId IN (<questIds>)
			ORDER BY questId, stepIndex, conditionIndex";

		private const string RewardsQuery =
			@"SELECT questId, name, itemId, collectId, quantity, quality, type
			FROM uesp_esolog.questReward
			WHERE questId IN (<questIds>);";
		#endregion

		#region Static Fields
		/* private static readonly Dictionary<int, string> questStepTypeTexts = new Dictionary<int, string>
		{
			[1] = "And",
			[2] = "Or",
			[3] = "End",
			[4] = "Branch",
		}; */

		private static readonly Dictionary<int, string> QuestTypeTexts = new Dictionary<int, string>
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
		};

		private static readonly Dictionary<int, string> RepeatTypeTexts = new Dictionary<int, string>
		{
			[0] = string.Empty,
			[1] = "Immediately",
			[2] = "Daily"
		};
		#endregion

		#region Constructors
		[JobInfo("Quests", "ESO")]
		public EsoQuests([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
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

		#region Protected Override Methods
		protected override void Main() => this.SavePages(this.LogName);

		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Getting wiki data");
			var wikiQuests = new TitleCollection(this.Site);
			wikiQuests.GetCategoryMembers("Online-Quests");

			Debug.WriteLine(wikiQuests.Contains("Online:Capture Farm"));

			this.StatusWriteLine("Getting quest data");
			var quests = new List<QuestData>(this.GetQuestData(wikiQuests));
			var allTitles = new TitleCollection(this.Site);
			foreach (var quest in quests)
			{
				allTitles.Add(quest.FullPageName);
			}

			var allPages = allTitles.Load(PageModules.Info);
			allPages.RemoveExists(false);
			var places = EsoGeneral.GetPlaces(this.Site);
			foreach (var quest in quests)
			{
				if (allPages.Contains(quest.FullPageName))
				{
					quest.FullPageName += " (quest)";
				}

				if (!string.IsNullOrEmpty(quest.Zone))
				{
					var place = places[quest.Zone];
					if (place != null)
					{
						while (!string.Equals(place.TypeText, "Zone", StringComparison.Ordinal) && place.Zone != null && places[place.Zone] is Place newZone)
						{
							place = newZone;
						}

						if (place.TitleName != null)
						{
							quest.Zone = place.TitleName;
						}
					}
				}

				this.Pages.Add(this.NewPage(quest));
			}

			this.ProgressMaximum = this.Pages.Count + 1;
			this.Progress++;
		}
		#endregion

		#region Private Static Methods
		private static IEnumerable<QuestData> GetDBQuests()
		{
			foreach (var row in EsoGeneral.RunQuery(QuestQuery))
			{
				yield return new QuestData(row);
			}
		}

		private static List<string> QuestObjectives(string objectiveType, List<Condition> conditions)
		{
			var retval = new List<string>();
			foreach (var condition in conditions)
			{
				if (condition.Text.Length > 0 && !string.Equals(condition.Text, "TRACKER GOAL TEXT", StringComparison.Ordinal))
				{
					var conditionText = condition.Text.TrimEnd(TextArrays.Colon);
					var fullText = $"{{{{Online Quest Objective|{objectiveType}|{conditionText}}}}}";
					if (!retval.Contains(fullText))
					{
						retval.Add(fullText);
					}
				}
			}

			return retval;
		}
		#endregion

		#region Private Methods
		private IEnumerable<QuestData> GetFilteredQuests(TitleCollection wikiQuests)
		{
			foreach (var quest in GetDBQuests())
			{
				var title = Title.FromName(this.Site, quest.FullPageName);
				var titleDisambig = new Title(title.Namespace, title.PageName + " (quest)");
				if (!wikiQuests.Contains(title) && !wikiQuests.Contains(titleDisambig))
				{
					var missing = true;
					foreach (var wikiQuest in wikiQuests)
					{
						var splitName = wikiQuest.PageName.Split(" (", StringSplitOptions.None);
						if (string.Compare(splitName[0], quest.Name, StringComparison.OrdinalIgnoreCase) == 0)
						{
							missing = false;
							break;
						}
					}

					if (missing)
					{
						yield return quest;
					}
				}
			}
		}

		private IEnumerable<QuestData> GetQuestData(TitleCollection wikiQuests)
		{
			var quests = this.GetFilteredQuests(wikiQuests);
			var questDict = new Dictionary<string, QuestData>(StringComparer.Ordinal);
			var questNames = new Dictionary<long, string>();
			foreach (var quest in quests)
			{
				questDict.TryAdd(quest.Name, quest);
				questNames.Add(quest.Id, quest.Name);
			}

			var whereText = string.Join(",", questNames.Keys);
			this.StatusWriteLine("Getting stage data");
			foreach (var row in EsoGeneral.RunQuery(StageQuery.Replace("<questIds>", whereText, StringComparison.Ordinal)))
			{
				var stage = new Stage(row);
				var questId = (long)row["questId"];
				var questName = questNames[questId];
				var stages = questDict[questName].Stages;

				stages.Add(stage);
			}

			this.StatusWriteLine("Getting condition data");
			foreach (var row in EsoGeneral.RunQuery(ConditionQuery.Replace("<questIds>", whereText, StringComparison.Ordinal)))
			{
				var condition = new Condition(row);
				var questId = (long)row["questId"];
				var stageId = (long)row["questStepId"];
				var questName = questNames[questId];
				var stages = questDict[questName].Stages;
				if (stages.Find(item => item.Id == stageId) is Stage stage)
				{
					stage.Conditions.Add(condition);
				}
			}

			this.StatusWriteLine("Getting rewards data");
			foreach (var row in EsoGeneral.RunQuery(RewardsQuery.Replace("<questIds>", whereText, StringComparison.Ordinal)))
			{
				var reward = new Reward(row);
				var questId = (long)row["questId"];
				var questName = questNames[questId];
				var rewards = questDict[questName].Rewards;
				if (rewards.Find(item => item.ItemId == reward.ItemId) == null)
				{
					rewards.Add(reward);
				}
			}

			return questDict.Values;
		}

		private Page NewPage(QuestData quest)
		{
			var locs = new SortedSet<string>();
			var mergedStages = this.MergeStages(quest, locs);
			var journalEntries = new List<string>();
			foreach (var kvp in mergedStages)
			{
				var split = kvp.Key.Split(TextArrays.At);
				if (split[0].Length > 2)
				{
					journalEntries.Add(split[0]);
				}

				journalEntries.AddRange(QuestObjectives(split[1], kvp.Value));
			}

			var rewardList = new List<string>();
			var xp = string.Empty;
			var gold = false;
			foreach (var reward in quest.Rewards)
			{
				switch (reward.RewardType)
				{
					case -1:
						xp = "{{ESO XP|}}";
						break;
					case 1:
						gold = true;
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

							rewardText += "}}";
						}

						rewardList.Add(rewardText);
						break;
				}
			}

			if (gold)
			{
				rewardList.Add("{{ESO Gold|?}} Gold");
			}

			var rewards = string.Join("<br>", rewardList);

			var sb = new StringBuilder()
				.AppendLine("{{Empty|quest}}{{ONQP Header")
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

			sb
				.AppendLine("{{Online Quest Header")
				.AppendLine("|ID=" + (quest.InternalId >= 0 ? quest.InternalId.ToStringInvariant() : string.Empty))
				.AppendLine("|type=" + QuestTypeTexts[quest.Type])
				.AppendLine("|image=")
				.AppendLine("|imgdesc=")
				.AppendLine("|description=")
				.AppendLine("|Zone=" + quest.Zone)
				.AppendLine("|Faction=")
				.AppendLine("|Obj=" + quest.Objective)
				.AppendLine("|Giver=")
				.AppendLine("|Loc=" + string.Join(", ", locs))
				.AppendLine("|Prereq=")
				.AppendLine("|Prev=")
				.AppendLine("|Next=")
				.AppendLine("|Conc=")
				.AppendLine("|Reward=" + rewards)
				.AppendLine("|XP=" + xp)
				.AppendLine("|Level=")
				.AppendLine("|Journal=" + quest.BackgroundText);
			if (quest.RepeatType > 0)
			{
				sb.AppendLine("|Repeatable=" + RepeatTypeTexts[quest.RepeatType]);
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

			var retval = Page.FromName(this.Site, quest.FullPageName ?? throw new InvalidOperationException());
			retval.Text = sb.ToString();
			return retval;
		}

		private Dictionary<string, List<Condition>> MergeStages(QuestData quest, SortedSet<string> locs)
		{
			var mergedStages = new Dictionary<string, List<Condition>>(StringComparer.Ordinal);
			foreach (var stage in quest.Stages)
			{
				if (!string.Equals(stage.Zone, "Tamriel", StringComparison.Ordinal) && !string.Equals(stage.Zone, quest.Zone, StringComparison.Ordinal))
				{
					var title = new Title(this.Site[UespNamespaces.Online], stage.Zone);
					locs.Add(title.AsLink(true));
				}

				var finishText = stage.FinishText;
				var stageText = '|' + finishText + '|' + stage.Text + '@' + (stage.Visibility == Visibility.Normal ? string.Empty : stage.Visibility.ToString().ToLowerInvariant());
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
		#endregion

		#region Private Classes
		private sealed class Condition : IEquatable<Condition>
		{
			#region Constructors
			public Condition(IDataRecord row)
			{
				this.IsComplete = (sbyte)row["isComplete"] == 1;
				this.IsFail = (sbyte)row["isFail"] == 1;
				this.Text = (string)row["text"];
			}
			#endregion

			#region Public Properties
			public bool IsComplete { get; }

			public bool IsFail { get; }

			public string Text { get; }

			public bool Equals(Condition? other) =>
				other != null &&
				this.IsComplete == other.IsComplete &&
				this.IsFail == other.IsFail &&
				string.Equals(this.Text, other.Text, StringComparison.Ordinal);
			#endregion

			#region Public Override Methods
			public override bool Equals(object? obj) => this.Equals(obj as Condition);

			public override int GetHashCode() => this.Text.GetHashCode(StringComparison.Ordinal) ^ (this.IsFail ? 1 : 0) ^ (this.IsComplete ? 2 : 0);
			#endregion
		}

		private class QuestData
		{
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
				this.Name = ((string)row["name"]).Replace("  ", " ", StringComparison.Ordinal); // Handles "Capture  Farm" with two spaces.
				this.FullPageName = "Online:" + this.Name;
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

			public string FullPageName { get; set; }

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

			public List<Reward> Rewards { get; } = new List<Reward>();

			public List<Stage> Stages { get; } = new List<Stage>();

			public int Type { get; }

			public string Zone { get; set; }
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion
		}

		private class Reward
		{
			public Reward(IDataRecord row)
			{
				this.CollectId = (int)row["collectId"];
				this.ItemId = (int)row["itemId"];
				this.RewardType = (short)row["type"];
				this.Name = (string)row["name"];
				this.Quality = (sbyte)row["quality"];
				this.Quantity = (int)row["quantity"];
			}

			public int CollectId { get; }

			public int ItemId { get; }

			public int RewardType { get; }

			public string Name { get; }

			public int Quality { get; }

			public int Quantity { get; }
		}

		private class Stage
		{
			#region Constructors
			public Stage(IDataRecord row)
			{
				this.Id = (long)row["id"];
				this.Text = (string)row["text"];
				this.Visibility = (Visibility)(sbyte)row["visibility"];
				this.Zone = (string)row["zone"];
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
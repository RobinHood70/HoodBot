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
			"uniqueQuest.id,\n" +
			"uniqueQuest.internalId,\n" +
			"uniqueQuest.name,\n" +
			"uniqueQuest.type,\n" +
			"uniqueQuest.repeatType,\n" +
			"uniqueQuest.backgroundText,\n" +
			"uniqueQuest.objective,\n" +
			"uniqueQuest.zone,\n" +
			"location.zone locationZone,\n" +
			"uniqueQuest.level,\n" +
			"uniqueQuest.goalText,\n" +
			"uniqueQuest.confirmText,\n" +
			"uniqueQuest.declineText,\n" +
			"uniqueQuest.endDialogText,\n" +
			"uniqueQuest.endJournalText,\n" +
			"uniqueQuest.endBackgroundText\n" +
		"FROM\n" +
			"uniqueQuest INNER JOIN\n" +
			"location ON uniqueQuest.locationId = location.id";

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
		"FROM questCondition\n" +
		"WHERE questId IN (<questIds>)\n" +
		"ORDER BY questId, stepIndex, conditionIndex";

		private const string RewardsQuery =
		"SELECT questId, name, itemId, collectId, quantity, quality, type\n" +
		"FROM questReward\n" +
		"WHERE questId IN (<questIds>);";

		private const string TemplateName = "Online Quest Header";
		#endregion

		#region Static Fields
		private static readonly Dictionary<long, string> PageNameOverrides = new()
		{
			[4151] = "A Bitter Pill (Deshaan)",
			[6042] = "A Bitter Pill (Clockwork City)",
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
		private readonly TitleCollection allTitles;
		private readonly bool overwriteMode;
		#endregion

		#region Constructors
		[JobInfo("Update Quests", "ESO Update")]
		public EsoQuests(JobManager jobManager, bool overwriteMode)
			: base(jobManager)
		{
			this.allTitles = new TitleCollection(this.Site);
			this.overwriteMode = overwriteMode;
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
			this.allTitles.Sort();
			this.GetFilteredQuests();
			if (this.quests.Count == 0)
			{
				return;
			}

			var sb = new StringBuilder();
			var idToQuest = new Dictionary<long, QuestData>();

			foreach (var quest in this.quests)
			{
				idToQuest.Add(quest.Value.Id, quest.Value);
				sb
					.Append(',')
					.Append(quest.Value.Id);
			}

			sb.Remove(0, 1);
			var whereText = sb.ToString();

			this.GetStages(idToQuest, whereText);
			this.GetConditions(idToQuest, whereText);
			this.GetRewards(idToQuest, whereText);
			this.GetPlaces();
		}

		protected override void LoadPages()
		{
			foreach (var quest in this.quests)
			{
				var page = this.Site.CreatePage(quest.Key, this.NewPageText(quest.Value));
				this.Pages.Add(page);
			}
		}

		#endregion

		#region Private Static Methods
		private static Title DisambigTitle(Title title) => TitleFactory.FromValidated(title.Site[UespNamespaces.Online], title.PageName + " (quest)");

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

		private static List<QuestData> GetQuests()
		{
			var questList = new List<QuestData>();
			foreach (var quest in Database.RunQuery(EsoLog.Connection, QuestQuery, row => new QuestData(row)))
			{
				questList.Add(quest);
			}

			return questList;
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
		private void ExcludeExisting(PageCollection pageInfo, HashSet<long> idExclusions, TitleCollection nameExclusions)
		{
			foreach (VariablesPage title in pageInfo)
			{
				if (title.GetVariable("ID") is string idText && long.TryParse(idText, System.Globalization.NumberStyles.None, this.Site.Culture, out var id))
				{
					idExclusions.Add(id);
				}

				nameExclusions.Add(title);
			}
		}

		private void ExcludeRedisambigs(List<QuestData> questList, PageCollection pageInfo, TitleCollection nameExclusions)
		{
			var addTitles = new TitleCollection(this.Site);
			foreach (var quest in questList)
			{
				var title = this.TitleFromQuest(quest);
				if (!pageInfo.Contains(title))
				{
					addTitles.Add(title);
				}

				var disambigTitle = DisambigTitle(title);
				if (!pageInfo.Contains(disambigTitle))
				{
					addTitles.Add(disambigTitle);
				}
			}

			if (addTitles.Count > 0)
			{
				pageInfo.Clear();
				pageInfo.GetTitles(addTitles);
				pageInfo.GetBacklinks("Online:Capture Keep", BacklinksTypes.Backlinks);
				pageInfo.GetBacklinks("Online:Capture Resource", BacklinksTypes.Backlinks);
				pageInfo.GetBacklinks("Online:Dark Brotherhood Contracts", BacklinksTypes.Backlinks);
				foreach (var page in pageInfo)
				{
					if (page.IsRedirect || (page.IsDisambiguation ?? false))
					{
						nameExclusions.Add(page);
					}
				}
			}
		}

		private void GetConditions(Dictionary<long, QuestData> idToQuest, string whereText)
		{
			this.StatusWriteLine("Getting condition data");
			var conditions = Database.RunQuery(EsoLog.Connection, ConditionQuery.Replace("<questIds>", whereText, StringComparison.Ordinal), row => new Condition(row));
			foreach (var condition in conditions)
			{
				var quest = idToQuest[condition.QuestId];
				if (quest.Stages.Find(item => item.Id == condition.StageId) is Stage stage)
				{
					stage.Conditions.Add(condition);
				}
			}
		}

		private void GetFilteredQuests()
		{
			var questList = GetQuests();
			var pageInfo = this.Site.CreateMetaPageCollection(PageModules.Info | PageModules.Properties, false, "ID");
			pageInfo.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);

			var removeQuests = new[] { "A Chance for Peace", "A Father's Pride", "A Final Peace", "A Mother's Request", "A New Venture", "A Sheep in Need", "A Special Reagent", "Aiding the Archipelago", "All Hands on Deck", "An Experiment with Peace", "Arcane Research", "Ascendant Shadows", "Avarice of the Eldertide", "Balki's Map Fragment", "Blood, Books, and Steel", "Buried at the Bay", "Cards Across the Continent", "Challenges of the Past", "Cold Blood, Old Pain", "Cold Trail", "Coral Conundrum", "Deadly Investigations", "Druidic Research", "Dueling Tributes", "Escape from Amenos", "Ferone's Map Fragment", "Green with Envy", "In Secret and Shadow", "Of Knights and Knaves", "People of Import", "Pirate Problems", "Prison Problems", "Pursuit of Freedom", "Race for Honor", "Reavers of the Reef", "Rhadh's Map Fragment", "Scalding Scavengers", "Seek and Destroy", "Spies in the Shallows", "Tales of Tribute (quest)", "The All Flags Curse", "The Ascendant Storm", "The Corrupted Grove", "The Final Round", "The Intoxicating Mix", "The Large Delegate", "The Long Game (High Isle)", "The Long Way Home", "The Lost Symbol", "The Missing Prowler", "The Princess Detective", "The Sable Knight (quest)", "The Serpent Caller", "The Tournament Begins", "To Catch a Magus", "Tournament of the Heart", "Tower Full of Trouble", "Venting the Threat", "Wildhorn's Wrath", "Wisdom of the Druids" };
			foreach (var quest in removeQuests)
			{
				pageInfo.Remove(TitleFactory.FromValidated(this.Site[UespNamespaces.Online], quest));
			}

			var idExclusions = new HashSet<long>();
			var nameExclusions = new TitleCollection(this.Site);
			this.ExcludeExisting(pageInfo, idExclusions, nameExclusions);
			this.ExcludeRedisambigs(questList, pageInfo, nameExclusions);
			Title? finalTitle = null;
			foreach (var quest in questList)
			{
				var title = this.TitleFromQuest(quest);
				var titleDisambig = DisambigTitle(title);
				if (!idExclusions.Contains(quest.InternalId) && !nameExclusions.Contains(title) && !nameExclusions.Contains(titleDisambig))
				{
					if (this.overwriteMode)
					{
						if (!this.allTitles.TryGetValue(titleDisambig, out finalTitle))
						{
							finalTitle = title;
						}
					}
					else
					{
						finalTitle = this.allTitles.TryGetValue(title, out finalTitle)
							? this.allTitles.Contains(titleDisambig)
								? throw new InvalidOperationException("Could not find valid page to add quest information to.")
								: titleDisambig
							: title;
					}
				}

				if (finalTitle is not null && !this.quests.TryAdd(finalTitle, quest))
				{
					var dupe = this.quests[finalTitle];
					this.Warn($"finalTitle [[{finalTitle.FullPageName}]] duplicated between [[{quest.Name}]] ({quest.Id}) and [[{dupe.Name}]] ({dupe.Id})");
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
					else
					{
						this.StatusWriteLine("Still need to check is Place.");
					}
				}
				else
				{
					this.StatusWriteLine("Still need to check .Value.Zone.");
				}
			}
		}

		private void GetRewards(Dictionary<long, QuestData> idToQuest, string whereText)
		{
			this.StatusWriteLine("Getting rewards data");
			var rewardData = Database.RunQuery(EsoLog.Connection, RewardsQuery.Replace("<questIds>", whereText, StringComparison.Ordinal), row => new Reward(row));
			var rewards = new List<Reward>();
			var previousRewards = new HashSet<long>();
			foreach (var reward in rewardData)
			{
				var quest = idToQuest[reward.QuestId];
				if (!previousRewards.Add(reward.ItemId))
				{
					quest.AddReward(reward);
				}
			}
		}

		private void GetStages(Dictionary<long, QuestData> idToQuest, string whereText)
		{
			this.StatusWriteLine("Getting stage data");
			var stages = Database.RunQuery(EsoLog.Connection, StageQuery.Replace("<questIds>", whereText, StringComparison.Ordinal), row => new Stage(row));
			foreach (var stage in stages)
			{
				var quest = idToQuest[stage.QuestId];
				quest.Stages.Add(stage);
			}
		}

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
			if (!string.IsNullOrEmpty(quest.EndJournalText) &&
				!journalEntries[^1].Contains(quest.EndJournalText, StringComparison.OrdinalIgnoreCase))
			{
				journalEntries.Add(quest.EndJournalText);
			}

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
				.Append("|ID=")
				.AppendLine(quest.InternalId >= 0 ? quest.InternalId.ToStringInvariant() : string.Empty)
				.Append("|type=")
				.AppendLine(questTypeText)
				.AppendLine("|image=")
				.AppendLine("|imgdesc=")
				.AppendLine("|description=")
				.Append("|Zone=")
				.AppendLine(quest.Zone)
				.AppendLine("|Faction=")
				.Append("|Obj=")
				.AppendLine(quest.Objective)
				.AppendLine("|Giver=")
				.Append("|Loc=")
				.AppendJoin(", ", locs)
				.AppendLine()
				.AppendLine("|Prereq=")
				.AppendLine("|Prev=")
				.AppendLine("|Next=")
				.AppendLine("|Conc=")
				.Append("|Reward=")
				.AppendLine(quest.GetRewardText())
				.Append("|XP=")
				.AppendLine(quest.XP)
				.Append("|Journal=")
				.AppendLine(quest.BackgroundText);
			if (!string.IsNullOrWhiteSpace(quest.EndBackgroundText) &&
				!string.Equals(quest.EndBackgroundText, quest.BackgroundText, StringComparison.OrdinalIgnoreCase))
			{
				sb
					.Append("\n\n'''End text''': ")
					.Append(quest.EndBackgroundText);
			}

			if (quest.RepeatType > 0)
			{
				sb.Append("|Repeatable=").AppendLine(RepeatTypes[quest.RepeatType]);
			}

			sb
				.AppendLine("}}")
				.AppendLine()
				.AppendLine("==Quick Walkthrough==")
				.AppendLine("<!-- Instructions: Provide a point-by-point list of the key tasks that need to be completed for this quest. Spoilers should be avoided in the quick walkthrough. -->");
			if (!string.IsNullOrEmpty(quest.GoalText))
			{
				sb
					.Append("# ")
					.Append(quest.GoalText);
			}

			sb
				.AppendLine()
				.AppendLine("==Detailed Walkthrough==")
				.Append("<!-- Instructions: The detailed walkthrough should provide full information about the quest, organized into paragraphs. Spoilers belong in this section.-->");
			sb
				.AppendLine("<!--")
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

		private Title TitleFromQuest(QuestData quest) => TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], quest.Name);

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
				this.Level = (sbyte)row["level"];
				this.ConfirmText = (string)row["confirmtext"];
				this.DeclineText = (string)row["declinetext"];
				this.EndBackgroundText = (string)row["endbackgroundtext"];
				this.EndDialogueText = (string)row["enddialogtext"];
				this.EndJournalText = (string)row["endjournaltext"];
				this.GoalText = (string)row["goaltext"];

				var zone = (string)row["zone"];
				if (zone.Length == 0 || string.Equals(zone, "Tamriel", StringComparison.Ordinal))
				{
					zone = (string)row["locationZone"];
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

			public string ConfirmText { get; }

			public string DeclineText { get; }

			public string EndBackgroundText { get; }

			public string EndDialogueText { get; }

			public string EndJournalText { get; }

			public string GoalText { get; }

			public long Id { get; }

			public int InternalId { get; }

			public sbyte Level { get; }

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
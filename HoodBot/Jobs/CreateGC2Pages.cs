namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using Ionic.Zlib;
	using RobinHood70.CommonCode;
	using RobinHood70.GC2.GC2Lib;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class CreateGC2Pages : EditJob
	{
		#region Constants
		private const string BrText = "<br>";
		private const string InfoboxName = "Field Infobox (GC2)";
		#endregion

		#region Static Fields
		private static readonly string[] FolderNames =
		{
			@"D:\Data\GC2\Original",
			@"D:\Data\GC2"
		};

		private static readonly string[] GemColors = { "Orange", "Yellow", "White", "Red", "Green", "Cyan", "Black", "Blue", "Purple" };

		private static readonly Dictionary<string, StageType> StageTypeLookups = new(StringComparer.Ordinal)
		{
			["NORMAL"] = StageType.Normal,
			["SECRET"] = StageType.Secret,
			["VISION"] = StageType.Vision,
			["STORY_RELATED"] = StageType.StoryRelated,
			["TOMECHAMBER_LOCATION"] = StageType.TomeChamber,
			["WIZTOWER_LOCATION"] = StageType.WizardTower
		};
		#endregion

		#region Fields
		private readonly bool original;
		#endregion

		#region Constructors
		[JobInfo("Create GC2 Pages")]
		public CreateGC2Pages(JobManager jobManager, bool originalVersion)
			: base(jobManager) => this.original = originalVersion;
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var fieldData = LoadFromFolder(this.original);
			this.StatusWriteLine("Files parsed");

			var titles = new TitleCollection(this.Site);
			foreach (var field in fieldData.Values)
			{
				titles.Add(new Title(this.Site.Namespaces[MediaWikiNamespaces.Main], Field.PageName(field.Name, this.original)));
			}

			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= this.Pages_PageLoaded;
		}

		protected override void Main() => this.SavePages("Update field page", true);
		#endregion

		#region Private Static Methods

		private static string CommaAndList(IReadOnlyList<string> list)
		{
			if (list.Count == 0)
			{
				return string.Empty;
			}

			var sb = new StringBuilder(list[0]);
			for (var i = 1; i < list.Count - 2; i++)
			{
				sb
					.Append(", ")
					.Append(list[i]);
			}

			if (list.Count > 1)
			{
				sb
					.Append(" and ")
					.Append(list[list.Count - 1]);
			}

			return sb.ToString();
		}

		private static FieldCollection LoadFromFolder(bool original)
		{
			var extras = new Dictionary<string, FieldExtras>(StringComparer.Ordinal);
			FieldExtras GetExtras(string field)
			{
				if (!extras.TryGetValue(field, out var extra))
				{
					extra = new FieldExtras();
					extras.Add(field, extra);
				}

				return extra;
			}

			var folder = FolderNames[original ? 0 : 1];
			var achievementText = File.ReadAllLines(Path.Combine(folder, "GC2 Field Achievements.txt"));
			foreach (var line in achievementText)
			{
				var split = line.Split('\t');
				var extra = GetExtras(split[0]);
				extra.Achievements.Add(split[1]);
			}

			var specialText = File.ReadAllLines(Path.Combine(folder, "GC2 Field Specials.txt"));
			foreach (var line in specialText)
			{
				var split = line.Split('\t');
				var extra = GetExtras(split[0]);
				var type = split[1];
				var data = split[2];
				switch (type)
				{
					case "disabled":
						extra.DisablesFlying = string.Equals(data, "first", StringComparison.Ordinal)
							? DisableType.UntilBeaten
							: DisableType.Always;
						break;
					case "journeynote":
						extra.JourneyNotes.Add(int.Parse(data, CultureInfo.InvariantCulture));
						break;
					case "resembles":
						extra.Resembles = data;
						break;
					default:
						if (string.Equals(data, "start", StringComparison.Ordinal))
						{
							extra.Introduces = type;
						}
						else
						{
							extra.FlyingOnes.Add(new Flying(type, int.Parse(data, CultureInfo.InvariantCulture)));
						}

						break;
				}
			}

			var fieldText = File.ReadAllText(Path.Combine(folder, "GC2 Field Comp.txt"));
			var matches = Field.FieldParser.Matches(fieldText);
			var fieldList = new List<Field>();
			foreach (Match match in matches)
			{
				var fieldName = match.Groups["field"].Value;
				var id = int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture);
				var sort = int.Parse(match.Groups["sort"].Value, CultureInfo.InvariantCulture);
				var stageType = StageTypeLookups[match.Groups["stagetype"].Value];
				var x =
					int.Parse(match.Groups["x1"].Value, CultureInfo.InvariantCulture) +
					int.Parse(match.Groups["x2sign"].Value + "0" + match.Groups["x2"].Value, CultureInfo.InvariantCulture) +
					int.Parse(match.Groups["x3sign"].Value + "0" + match.Groups["x3"].Value, CultureInfo.InvariantCulture);
				var y =
					int.Parse(match.Groups["y1"].Value, CultureInfo.InvariantCulture) +
					int.Parse(match.Groups["y2sign"].Value + "0" + match.Groups["y2"].Value, CultureInfo.InvariantCulture) +
					int.Parse(match.Groups["y3sign"].Value + "0" + match.Groups["y3"].Value, CultureInfo.InvariantCulture);
				var fieldData = match.Groups["fielddata"].Value;
				fieldData = Encoding.ASCII.GetString(FromBase64ToBytes(fieldData)).Trim();
				extras.TryGetValue(fieldName, out var extraFieldInfo);
				var field = new Field(
						fieldName,
						id,
						sort,
						stageType,
						x,
						y,
						fieldData,
						extraFieldInfo);
				fieldList.Add(field);
			}

			var fieldCollection = new FieldCollection(fieldList);
			if (!original)
			{
				var originalFile = LoadFromFolder(true);
				foreach (var field in fieldCollection)
				{
					field.SteamExclusive = !originalFile.ContainsKey(field.Name);
				}
			}

			return fieldCollection;
		}

		#endregion

		#region Private Methods
		private static byte[] FromBase64ToBytes(string dataIn)
		{
			byte[] retval;
			var data = Convert.FromBase64String(dataIn);
			using (var compressedStream = new MemoryStream(data, 2, data.Length - 2)) // Strip off first 2 bytes, which are zip format specifier.
			using (var zipStream = new DeflateStream(compressedStream, CompressionMode.Decompress)) // Used to come from System.IO.Compression, now comes from Ionic.Zlib - still works?
			using (var memoryStream = new MemoryStream())
			{
				zipStream.CopyTo(memoryStream);
				retval = memoryStream.ToArray();
			}

			return retval;
		}

		private void Pages_PageLoaded(PageCollection sender, Page eventArgs) => throw new NotImplementedException();

		private string ToPercent(double value)
		{
			// We can't just use the format specifier because it doesn't do rounding.
			value = Math.Round(value * 100, 2);
			return value.ToString(this.Site.Culture);
		}

		private void UpdateWikiText(Page page, Field field, FieldCollection fieldData)
		{
			var parser = new ContextualParser(page);
			this.UpdateTemplateText(parser, field, fieldData);

			var difficulty = "Looming";
			var fieldName = field.IsCompass
				? $"[[Field {Field.PageName(field.AccessedFrom, this.original)}|Field {field.AccessedFrom}]]"
				: "the [[Mysterious Compass (GC2)|Mysterious Compass]]";
			if (!field.IsCompass && field.AccessedFrom.Length > 0)
			{
				if (field.IsSecret)
				{
					difficulty = "Glaring";
				}
			}

			parser.AppendText(FormattableString.Invariant($"Field {field.Name} is obtained by completing {fieldName} in [[Gemcraft Chapter 2 (Chasing Shadows)|Gemcraft Chapter 2]] on {difficulty} difficulty. "));

			if (field.Buildings.TomeChamber != null)
			{
				parser.AppendText("To open the tome chamber on this field, you must kill " + field.Buildings.TomeChamber.SpecificName);
			}

			var textTypes = new List<string>();
			foreach (var gemType in field.InitScript.GemTypes)
			{
				var color = GemColors[gemType];
				var typeText = Gem.GemTypes[gemType];
				textTypes.Add(string.Concat("[[", color, " Gems|", typeText, "]]"));
			}

			var gemTypesText = CommaAndList(textTypes);
			if (field.InitScript.GemTypes.Count == 1)
			{
				parser.AppendText(string.Concat("The only guaranteed gem type available for this field is ", gemTypesText, ". "));
			}
			else if (field.InitScript.GemTypes.Count == 9)
			{
				parser.AppendText("All gem types are available for this field. ");
			}
			else
			{
				parser.AppendText(string.Concat("The guaranteed gem types available for this field include ", gemTypesText, ". "));
			}

			parser.AppendText(FormattableString.Invariant($"There are {field.MonsterData.WavesNum} waves on this field"));
			switch (field.Achievements.Count)
			{
				case 0:
					break;
				case 1:
					parser.AppendText(" and one achievement exclusive to it");
					break;
				case 2:
					parser.AppendText(" and two achievements exclusive to it");
					break;
				default:
					throw new InvalidOperationException("Unexpected number of achievements.");
			}

			if (field.Resembles != null)
			{
				parser.AppendText(string.Concat(". This field resembles ", field.Resembles));
			}

			parser.AppendLine(".\n");

			parser.AppendLine("==Starting Out==");
			if (field.Type == StageType.Vision)
			{
				parser.AppendText(FormattableString.Invariant($"You start with {field.InitScript.Mana:#,0} mana"));
				if (field.InitScript.EnabledSpells != null)
				{
					if (field.InitScript.EnabledSpells.Count == 0)
					{
						parser.AppendText(" and all spells are disabled");
					}
					else if (field.InitScript.EnabledSpells.Count == 6)
					{
						parser.AppendText(" and all spells are enabled");
					}
					else
					{
						parser.AppendText(string.Concat(" and the following spells are enabled: ", CommaAndList(field.InitScript.EnabledSpells)));
					}

					if (field.InitScript.DisabledSpells.Count > 0)
					{
						var list = CommaAndList(field.InitScript.DisabledSpells);
						parser.AppendText(FormattableString.Invariant($". The following abilities are disabled: {list}"));
					}
				}

				parser.AppendText(". ");
			}

			if (field.Buildings.PlayerBuildingCounts.Count + field.Buildings.OtherBuildingCounts.Count + field.InitScript.StartGems.Count == 0)
			{
				parser.AppendText("There are no buildings or gems present when you start the field. ");
			}
			else
			{
				if (field.Type == StageType.Vision)
				{
					parser.AppendLine();
				}

				parser.AppendText("{| class=\"article-table\"\n");
				var text = string.Empty;
				if (field.Buildings.PlayerBuildingCounts.Count > 0)
				{
					text += "!! Player Buildings ";
				}

				if (field.Buildings.OtherBuildingCounts.Count > 0)
				{
					text += "!! Other Buildings ";
				}

				if (field.InitScript.StartGems.Count > 0)
				{
					text += "!! Gems ";
				}

				parser.AppendText(text[1..]);
				parser.AppendText("\n|- style=\"vertical-align:top\"");

				if (field.Buildings.PlayerBuildingCounts.Count > 0)
				{
					parser.AppendText("\n| ");
					var sb = new StringBuilder();
					foreach (var building in field.Buildings.PlayerBuildingCounts)
					{
						sb.Append(string.Concat(BrText, building.Type, building.Count > 1 ? " ×" + building.Count.ToStringInvariant() : string.Empty));
					}

					sb.Remove(0, 4);
					parser.AppendText(sb.ToString());
				}

				if (field.Buildings.OtherBuildingCounts.Count > 0)
				{
					parser.AppendText("\n| ");
					var sb = new StringBuilder();
					foreach (var building in field.Buildings.OtherBuildingCounts)
					{
						sb.Append(string.Concat(BrText, building.SpecificName, building.Count > 1 ? " ×" + building.Count.ToStringInvariant() : string.Empty));
					}

					sb.Remove(0, 4);
					parser.AppendText(sb.ToString());
				}

				if (field.InitScript.StartGems.Count > 0)
				{
					parser.AppendText("\n| ");
					var sb = new StringBuilder();
					foreach (var gem in field.InitScript.StartGems)
					{
						sb.Append(string.Concat(BrText, gem.Key, gem.Value > 1 ? " ×" + gem.Value.ToStringInvariant() : string.Empty));
					}

					sb.Remove(0, 4);
					parser.AppendText(sb.ToString());
				}

				parser.AppendText("\n|}");
			}

			parser.AppendLine("\n\n==Waves==");
			parser.AppendLine("The following table applies only to Looming difficulty with no battle traits.");
			parser.AppendLine("{{Wave Table (GC2)/Start}}");
			foreach (var wave in field.Warks)
			{
				var t = parser.Nodes.Factory.TemplateNodeFromWikiText(FormattableString.Invariant($"{{{{Wave Table (GC2)/Row|{wave.Number}|{wave.NumMonsters}|{wave.TypeModifier}|{wave.Type}|{wave.HitPoints:#,0}|{wave.HitPointRegen:#,0}|{wave.Armor:#,0}|{wave.SpeedText}|{wave.ManaBase:#,0}|{wave.ManaBase * wave.CostToBanishMultiplier:#,0}|{wave.XpText}}}}}"));
				if (wave.BuffTexts != null)
				{
					for (var i = 1; i <= wave.BuffTexts.Count; i++)
					{
						t.Add("buff" + i.ToStringInvariant(), wave.BuffTexts[i - 1]);
					}
				}

				if (wave.Spark != null)
				{
					t.Add("spark", wave.Spark.Text);
				}

				parser.Nodes.Add(t);
			}

			parser.AppendLine("{{Wave Table (GC2)/End}}\n");

			if (field.Flying.Count > 0 || field.Flying.Disabled > DisableType.Never)
			{
				parser.AppendLine("==Flying Ones==");
				if (field.Flying.Introduced != null)
				{
					parser.AppendText(field.Flying.Introduced.IntroText + ". ");
				}

				switch (field.Flying.Disabled)
				{
					case DisableType.UntilBeaten:
						parser.AppendText("Random flying ones are disabled on this field until you have beaten it at least once. ");
						break;
					case DisableType.Always:
						parser.AppendText("Random flying ones are always disabled on this field. ");
						break;
					default:
						break;
				}

				if (field.Flying.Count > 0)
				{
					parser.AppendText("Flying ones always appear as listed below, regardless of difficulty level or battle traits. Note that waves and sparks are both counted when determining when a flying one will appear.");
					foreach (var flying in field.Flying)
					{
						parser.AppendText("\n* " + flying.GetText(field.Type == StageType.Vision));
					}
				}

				parser.AppendLine("\n");
			}

			if (field.Achievements.Count > 0)
			{
				parser.AppendLine("==Achievements==");
				parser.AppendText(FormattableString.Invariant($"Achievements exclusive to Field {field.Name} include:\n"));
				foreach (var achievement in field.Achievements)
				{
					parser.AppendText(FormattableString.Invariant($"* {achievement}\n"));
				}

				parser.AppendLine();
			}

			parser.AppendLine("==Completion==");
			if (field.Buildings.Orb.Drops == null && field.JourneyNotes.Count == 0)
			{
				parser.AppendLine("There are no drops for completing this field.");
			}
			else
			{
				parser.AppendLine("The player receives the following drops upon completing this field:");
				if (field.Buildings.WizardTower != null && field.Buildings.WizardTower.Drops != null)
				{
					foreach (var drop in field.Buildings.WizardTower.Drops)
					{
						var glaring = false;
						if (drop is FieldToken ft)
						{
							glaring = fieldData[ft.FieldName].IsSecret;
						}

						parser.AppendText(string.Concat("* ", drop.LongText(this.original), glaring ? " (glaring only)" : string.Empty, "\n"));
					}
				}

				if (field.Buildings.Orb.Drops != null && field.Buildings.Orb.Drops.Count > 0)
				{
					foreach (var drop in field.Buildings.Orb.Drops)
					{
						var glaring = false;
						if (drop is FieldToken ft)
						{
							glaring = fieldData[ft.FieldName].IsSecret;
						}

						parser.AppendText(string.Concat("* ", drop.LongText(this.original), glaring ? " (glaring only)" : string.Empty, "\n"));
					}
				}

				if (field.JourneyNotes.Count > 0)
				{
					var plural = field.JourneyNotes.Count > 1 ? "s" : string.Empty;
					parser.AppendText(FormattableString.Invariant($"* {field.JourneyNotes.Count} Journey Note{plural}\n"));
					parser.AppendLine("<gallery>");
					foreach (var note in field.JourneyNotes)
					{
						var journeyDesc = JourneyNote.Descriptions.ContainsKey(note)
							? "|" + JourneyNote.Descriptions[note]
							: string.Empty;
						parser.AppendText(string.Concat("Journey Note ", note, ".png", journeyDesc, "\n"));
					}

					parser.AppendLine("</gallery>");
				}

				if (field.Buildings.TomeChamber != null)
				{
					parser.AppendLine();
					parser.AppendLine("In addition, if the player opens the tome chamber, they will get the following drops:");
					foreach (var drop in field.Buildings.TomeChamber.Drops)
					{
						var glaring = false;
						if (drop is FieldToken ft)
						{
							glaring = fieldData[ft.FieldName].IsSecret;
						}

						var glaringText = glaring ? " (glaring only)" : string.Empty;
						parser.AppendText(string.Concat("* ", drop.LongText(this.original), glaringText, "\n"));
					}
				}
			}

			parser.AppendLine();
			parser.AppendText("[[Category:Gemcraft Chapter 2 Levels]]\n[[Category:Levels]]\n[[Category:Gemcraft Chapter 2 (Chasing Shadows)]]");

			page.Text = parser.ToRaw();
		}

		private void UpdateTemplateText(ContextualParser parser, Field field, FieldCollection fieldData)
		{
			if (parser.FindTemplate(InfoboxName) is not ITemplateNode infobox)
			{
				infobox = parser.Nodes.Factory.TemplateNodeFromParts(InfoboxName);
				parser.Nodes.Insert(0, infobox);
			}

			infobox.AddOrChange("field", field.Name);
			if (field.IsVision)
			{
				infobox.Add("hextile", field.Hextile.ToString());
			}

			infobox.AddOrChange("fieldimage", string.Empty);

			if (field.Type is not StageType.Normal and not StageType.Secret)
			{
				infobox.Add("fieldtype", field.TypeFriendly);
			}

			infobox.Add("unlockedby", field.AccessedFrom ?? "Start");
			if (field.IsSecret && !field.IsCompass)
			{
				infobox.Add("unlockedbyglaring", "1");
			}

			var unlocksList = new List<string>();
			if (field.Buildings.Orb.Drops != null)
			{
				var fieldList = new List<string>();
				foreach (var drop in field.Buildings.Orb.Drops)
				{
					if (drop is FieldToken)
					{
						fieldList.Add(drop.ShortText(this.original));
					}
					else
					{
						unlocksList.Add(drop.ShortText(this.original));
					}
				}

				fieldList.Sort(StringComparer.Ordinal);
				for (var unlockNum = 1; unlockNum <= fieldList.Count; unlockNum++)
				{
					var unlockField = fieldList[unlockNum - 1];
					infobox.Add("unlocks" + unlockNum.ToStringInvariant(), unlockField);

					var uf = fieldData[unlockField];
					if (uf.IsSecret && fieldData[uf.AccessedFrom].IsCompass)
					{
						infobox.Add("unlocks" + unlockNum.ToStringInvariant() + "glaring", "1");
					}
				}
			}

			foreach (var note in field.JourneyNotes)
			{
				unlocksList.Add("Journey Note " + note.ToStringInvariant());
			}

			if (unlocksList.Count > 0)
			{
				infobox.Add("unlocksother", string.Join(BrText, unlocksList));
			}

			infobox.Add("availgems", string.Join(string.Empty, field.InitScript.GemTypes));
			infobox.Add("rainchance", this.ToPercent(field.RainChance));
			infobox.Add("snowchance", this.ToPercent(field.SnowChance));

			if (field.Buildings.TomeChamber != null)
			{
				var drops = new List<string>();
				foreach (var drop in field.Buildings.TomeChamber.Drops)
				{
					drops.Add(drop.ShortText(this.original));
				}

				infobox.Add("tomechamber", string.Join(", ", drops));
			}

			if (field.Buildings.WizardTower != null && field.Buildings.WizardTower.Drops != null)
			{
				var drops = new List<string>();
				foreach (var drop in field.Buildings.WizardTower.Drops)
				{
					drops.Add(drop.ShortText(this.original));
				}

				infobox.Add("wiztower", string.Join(", ", drops));
			}

			var monsterData = field.MonsterData;
			infobox.Add("reavers", (monsterData.WavesNum - monsterData.GiantWavesNum - monsterData.SwarmlingWavesNum).ToStringInvariant());
			infobox.Add("giants", monsterData.GiantWavesNum.ToStringInvariant());
			infobox.Add("swarmlings", monsterData.SwarmlingWavesNum.ToStringInvariant());
			infobox.Add("hp", monsterData.HpFirstWave.ToStringInvariant());
			infobox.Add("hpmult", monsterData.HpMult.ToStringInvariant());
			infobox.Add("armor", monsterData.ArmorFirstWave.ToStringInvariant());
			infobox.Add("armorinc", monsterData.ArmorIncrement.ToStringInvariant());
			infobox.Add("buffinc", this.ToPercent(monsterData.BuffValIncrement));
			infobox.Add("buffstart", monsterData.FirstBuffedWave.ToStringInvariant());

			if (!this.original)
			{
				infobox.Add("steam", field.SteamExclusive ? "only" : "yes");
			}

			parser.Nodes.Add(infobox);
		}
		#endregion
	}
}

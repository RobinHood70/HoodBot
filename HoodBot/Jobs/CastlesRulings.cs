namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed partial class CastlesRulings : EditJob
	{
		#region Static Fields
		private static readonly CultureInfo GameCulture = new("en-US");
		private static readonly string[] RulingsGroupNames = ["_requiredRulings", "_randomRulings", "_personalRulings", "_instantRulings", "_rewardRulings"];
		private static readonly Dictionary<string, string> StyleReplacements = new(StringComparer.Ordinal)
		{
			["item"] = "A7762E",
			["highlight"] = "A7762E",
			["prop"] = "C69C5F",
			["joke"] = "7B2235",
			["positive"] = "49790C",
			["negative"] = "BC322E",
			["fire"] = "BF4A26",
			["frost"] = "385D82",
			["shock"] = "512E55",
		};

		private static readonly Dictionary<string, string> TxInfoOverrides = new(StringComparer.Ordinal)
		{
			["BookTitles"] = "{{Hover|{0}|<random book title>}}",
			["FirstEdition_Variations"] = "{{Hover|{0}|first edition}}",
			["INN001_EstablishmentName"] = "<random inn name>",
			["PR006_Joke_Variations"] = "<random joke>",
			["PR020_Dish_Variations"] = "{{Hover|{0}|<random dish>}}",
			["RoyalAddress"] = "{{Hover|{0}|<Royal Address>}}",
			["SR030_RulerFamilyMember"] = "{{Hover|{0}|<random family member>}}",
		};

		private static readonly bool WikiMode = true;
		#endregion

		#region Fields
		private readonly CastlesData data;
		private readonly Dictionary<string, Ruling> rulings = new(StringComparer.Ordinal);
		private readonly CastlesTranslator translator;
		#endregion

		#region Constructors
		[JobInfo("Castles Rulings", "|Castles")]
		public CastlesRulings(JobManager jobManager)
			: base(jobManager)
		{
			this.translator = new CastlesTranslator(GameCulture);
			this.data = new CastlesData(this.translator);
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			if (WikiMode)
			{
				foreach (var (key, value) in TxInfoOverrides)
				{
					this.translator.ParserOverrides[key] = value;
				}
			}

			this.GetRulingGroups();
			this.WriteRulingGroups(@"D:\Castles\Rulings.txt");
		}

		protected override string GetEditSummary(Page page) => "Update rulings";

		protected override void LoadPages()
		{
			this.LoadAndSortPages();
			this.CreateRulingsPages();
		}

		protected override void PageLoaded(Page page)
		{
			var rulingName = page.Title.SubPageName();
			var ruling = this.rulings[rulingName];

			var parser = new SiteParser(page);
			var template = parser.FindSiteTemplate("Castles Ruling") ?? throw new InvalidOperationException();
			var textParam = template.Find("text") ?? throw new InvalidOperationException();
			var conditionsParam = template.Find("conditions") ?? throw new InvalidOperationException();
			var choiceParam = template.Find("choices") ?? throw new InvalidOperationException();

			textParam.SetValue(ruling.Text, ParameterFormat.Copy);
			if (ruling.Conditions.Count > 0)
			{
				conditionsParam.SetValue(string.Join("<br>\n", ruling.Conditions), ParameterFormat.Copy);
			}

			foreach (var choiceTemplate in choiceParam.Value.FindAll<SiteTemplateNode>())
			{
				var idParam = choiceTemplate.Find("id") ?? throw new InvalidOperationException();
				var effectsParam = choiceTemplate.Find("effects") ?? throw new InvalidOperationException();
				var id = int.Parse(idParam.Value.ToValue(), GameCulture);
				if (ruling.Choices.TryGetValue(id, out var choice))
				{
					choice.Effects = effectsParam.Value.ToRaw().Trim();
				}
			}

			var sb = new StringBuilder();
			foreach (var choice in ruling.Choices)
			{
				BuildChoice(sb, choice);
			}

			choiceParam.SetValue(sb.ToString(), ParameterFormat.Copy);
			parser.UpdatePage();
		}

		protected override void PageMissing(Page page) => page.Text =
			"{{Castles Ruling\n" +
			"|text=\n" +
			"|conditions=\n" +
			"|choices=\n" +
			"}}";
		#endregion

		#region Private Static Methods
		[GeneratedRegex(@"<style=(?<style>\w+)>(?<content>.*?)</style>", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, 10000)]
		private static partial Regex CastlesStyleReplacer();

		private static string StyleReplacer(Match match)
		{
			var style = match.Groups["style"].Value;
			return !StyleReplacements.TryGetValue(style, out var colour)
				? match.Value
				: $"{{{{FC|#{colour}|{match.Groups["content"].Value}}}}}";
		}
		#endregion

		#region Private Methods
		private static string BuildChoice(StringBuilder sb, Choice choice)
		{
			sb
				.Append("{{Castles Ruling/Choice")
				.Append("\n|id=")
				.Append(choice.Id)
				.Append("\n|text=")
				.AppendJoin(" ''or''<br>\n", choice.SubChoices);
			if (choice.Conditions.Count > 0)
			{
				sb.Append("\n|conditions=")
				.AppendJoin("<br>\n", choice.Conditions);
			}

			sb
				.Append("\n|effects=")
				.Append(choice.Effects);
			if (choice.EffectFlags.Count > 0)
			{
				sb.Append("\n|flageffects=")
				.AppendJoin("<br>\n", choice.EffectFlags);
			}

			sb.Append("}}\n");

			return sb.ToString();
		}

		private void CreateRulingsPages()
		{
			var groups = this.RulingsToGroups();
			var sb = new StringBuilder();
			foreach (var groupName in RulingsGroupNames)
			{
				var friendly = groupName
					.Replace("_", string.Empty, StringComparison.Ordinal)
					.Replace("Rulings", string.Empty, StringComparison.Ordinal)
					.UpperFirst(GameCulture);
				sb.Append($"=={friendly}==\n");
				var group = groups[groupName];
				foreach (var ruling in group)
				{
					sb
						.Append("{{")
						.Append(ruling.PageName)
						.Append("}}\n");
				}

				sb.Append('\n');
			}

			this.Pages.Add(this.Site.CreatePage("Castles:Rulings", sb.ToString()));
		}

		private Ruling GetRuling(string group, dynamic rulingObject)
		{
			var descId = (string)rulingObject._rulingDescription;
			if (this.translator.GetSentence(descId) is not string translated)
			{
				translated = descId + CastlesData.NotFound;
			}

			var text = this.translator.Parse(translated, true);
			if (WikiMode)
			{
				text = CastlesStyleReplacer().Replace(text, StyleReplacer);
			}

			var conditions = new CastlesConditions(this.data, GameCulture);
			conditions.AddRulingInfo(rulingObject);
			var choices = new Choices();
			foreach (var choiceObject in rulingObject._rulingChoices)
			{
				choices.Add(new Choice(choiceObject, this.data, this.translator));
			}

			var name = (string)rulingObject._debugRulingName;
			name = WikiTextUtilities.DecodeAndNormalize(name);
			return new Ruling(group, name, text, conditions, choices);
		}

		private Dictionary<string, dynamic> GetRulingGroups()
		{
			using var file = File.OpenRead(@"D:\Castles\MonoBehaviour\RulingsDefault2.json");
			using var reader = new StreamReader(file);
			using var jsonReader = new JsonTextReader(reader);

			var retVal = new Dictionary<string, dynamic>(StringComparer.Ordinal);
			var serializer = new JsonSerializer();
			dynamic obj = serializer.Deserialize(jsonReader) ?? throw new InvalidOperationException();
			foreach (var rulingsGroupName in RulingsGroupNames)
			{
				var rulingList = new SortedList<string, Ruling>(StringComparer.Ordinal);
				var group = obj[rulingsGroupName];
				if (group is not null)
				{
					foreach (var rulingObject in group)
					{
						var localRuling = this.GetRuling(rulingsGroupName, rulingObject);
						rulingList.Add(localRuling.Name, localRuling);
						this.rulings.Add(localRuling.Name, localRuling);
					}
				}
			}

			return retVal;
		}

		private void LoadAndSortPages()
		{
			var titles = new TitleCollection(this.Site);
			foreach (var (_, ruling) in this.rulings)
			{
				titles.Add(ruling.PageName);
			}

			this.Pages.GetTitles(titles);
			this.Pages.Sort();
		}

		private Dictionary<string, List<Ruling>> RulingsToGroups()
		{
			var retval = new Dictionary<string, List<Ruling>>(StringComparer.Ordinal);
			foreach (var page in this.Pages)
			{
				var ruling = this.rulings[page.Title.SubPageName()];
				if (!retval.TryGetValue(ruling.Group, out var rulingList))
				{
					rulingList = [];
					retval[ruling.Group] = rulingList;
				}

				rulingList.Add(ruling);
			}

			return retval;
		}

		private void WriteRulingGroups(string fileName)
		{
			using var memStream = new MemoryStream();
			using var baseStream = File.CreateText(fileName);
			using var stream = new IndentedTextWriter(baseStream);
			var rulingGroups = new Dictionary<string, List<Ruling>>(StringComparer.Ordinal);
			foreach (var (rulingName, ruling) in this.rulings)
			{
				if (!rulingGroups.TryGetValue(ruling.Group, out var rulingList))
				{
					rulingList = [];
					rulingGroups[ruling.Group] = rulingList;
				}

				rulingList.Add(ruling);
			}

			foreach (var (groupName, group) in rulingGroups)
			{
				stream.WriteLine(groupName);
				stream.Indent++;
				foreach (var ruling in group)
				{
					stream.WriteLine(ruling.Text);
					stream.Indent++;
					foreach (var entry in ruling.Conditions)
					{
						stream.WriteLine(entry);
					}

					foreach (var choice in ruling.Choices)
					{
						for (var i = 0; i < choice.SubChoices.Count; i++)
						{
							var start = i == 0 ? "* " : "  ";
							var end = i < (choice.SubChoices.Count - 1) ? " <OR>" : string.Empty;
							var subChoice = choice.SubChoices[i];
							stream.WriteLine(start + subChoice + end);
						}

						stream.Indent++;
						foreach (var entry in choice.Conditions)
						{
							stream.WriteLine(entry);
						}

						foreach (var entry in choice.EffectFlags)
						{
							stream.WriteLine(entry);
						}

						stream.Indent--;
					}

					stream.Indent--;
					stream.WriteLine();
				}

				stream.Indent--;
			}
		}
		#endregion

		#region Private Classes
		private sealed class Choice
		{
			#region Fields
			private readonly CastlesData data;
			#endregion

			#region Constructors
			public Choice(dynamic choiceObject, CastlesData data, CastlesTranslator translator)
			{
				this.data = data;
				this.Id = (int)choiceObject._rulingChoiceTemplateUid.id;
				var choiceShort = (string)choiceObject._rulingChoiceDescription;
				if (translator.GetSentence(choiceShort) is not string choiceDesc)
				{
					choiceDesc = choiceShort + CastlesData.NotFound;
				}

				var text = translator.Parse(choiceDesc, true);
				if (WikiMode)
				{
					text = CastlesStyleReplacer().Replace(text, StyleReplacer);
				}

				this.SubChoices.AddRange(text.Split("<newline>"));
				var conditions = new CastlesConditions(this.data, GameCulture);
				conditions.AddChoiceInfo(choiceObject);
				this.Conditions = conditions;
				this.EffectFlags = conditions.EffectFlags;
			}
			#endregion

			#region Public Properties
			public List<string> Conditions { get; }

			public List<string> EffectFlags { get; }

			public string? Effects { get; set; }

			public int Id { get; }

			public List<string> SubChoices { get; } = [];
			#endregion
		}

		private sealed class Choices : KeyedCollection<int, Choice>
		{
			protected override int GetKeyForItem(Choice item) => item.Id;
		}

		private sealed class Ruling(string group, string name, string text, List<string> conditions, Choices choices)
		{
			public Choices Choices { get; } = choices;

			public List<string> Conditions { get; } = conditions;

			public string Group { get; } = group;

			public string Name { get; } = name;

			public string PageName => "Castles:Rulings/" + this.Name;

			public string Text { get; } = text;
		}
		#endregion
	}
}
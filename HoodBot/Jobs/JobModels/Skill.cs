namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	#region Public Enumerations
	internal enum ChangeType
	{
		None,
		Minor,
		Major
	}
	#endregion

	internal abstract class Skill
	{
		#region Public Constants
		public const string SummaryTemplate = "Online Skill Summary";
		#endregion

		#region Static Fields
		private static readonly HashSet<string> DestructionExceptions = new(StringComparer.Ordinal) { "Destructive Touch", "Impulse", "Wall of Elements" };
		private static readonly string[] DestructionTypes = ["Frost", "Shock", "Fire"];
		private static readonly string[] DoubleColonSplit = ["::"];
		#endregion

		#region Constructors
		protected Skill(IDataRecord row)
		{
			this.Name = EsoLog.ConvertEncoding((string)row["baseName"]);
			var classLine = EsoLog.ConvertEncoding((string)row["skillTypeName"]).Split(DoubleColonSplit, StringSplitOptions.None);
			var classValue = classLine[0];
			this.Class = classValue.OrdinalEquals("Craft")
				? classValue
				: "Crafting";
			this.SkillLine = classLine[1];
			if (!ReplacementData.SkillNameFixes.TryGetValue(this.Name, out var newName))
			{
				ReplacementData.SkillNameFixes.TryGetValue($"{this.Name} - {this.SkillLine}", out newName);
			}

			this.PageName = "Online:" + (newName ?? this.Name);
		}
		#endregion

		#region Public Static Properties
		public static Regex Highlight => new(@"(?:\|c[0-9a-fA-F]{6})+|(?:\|r)+", RegexOptions.None, Globals.DefaultRegexTimeout);

		public static Regex HighlightVar => new(@"\s*([\d]+(\.\d+)?|\|c[0-9A-Fa-f]{6}[^\|]+?\|r)\s*", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

		public static SortedList<string, string> IconNameCache { get; } = new(StringComparer.Ordinal);
		#endregion

		#region Public Properties
		public ChangeType ChangeType { get; protected set; }

		public string PageName { get; }
		#endregion

		#region Protected Properties
		protected string Class { get; set; }

		protected string Name { get; }

		protected string SkillLine { get; set; }
		#endregion

		#region Public Methods
		public string? UpdatePageText(Page page)
		{
			if (page.IsMissing)
			{
				page.Text =
					"{{Minimal|Skill}}";
				if (GameInfo.Eso.ModTemplateName.Length > 0)
				{
					page.Text += GameInfo.Eso.ModHeader + '\n';
				}

				page.Text +=
					"{{" + SummaryTemplate + "}}\n" +
					"\n" +
					"<!--\n" +
					"==Notes==\n" +
					"* -->\n" +
					"{{Stub}}\n" +
					"{{Online Skills " + this.Class + "}}";
			}

			SiteParser oldPage = new(page);
			SiteParser parser = new(page);
			List<SiteTemplateNode> skillSummaries = new(parser.FindSiteTemplates(SummaryTemplate));
			if (skillSummaries.Count != 1)
			{
				return "Incorrect number of {{" + SummaryTemplate + "}} matches on " + this.PageName;
			}

			var template = skillSummaries[0];
			template.TitleNodes.Trim();
			template.TitleNodes.AddText("\n");
			template.RemoveDuplicates();
			template.Remove("update");

			template.Update("line", this.SkillLine, ParameterFormat.OnePerLine, true);
			var iconValue = MakeIcon(this.SkillLine, this.Name);

			// Special cases
			if (iconValue.OrdinalEquals("Woodworking-Woodworking"))
			{
				iconValue = "Woodworking-Woodworking Skill";
			}

			if (this.Name.StartsWith("Keen Eye: ", StringComparison.Ordinal))
			{
				iconValue = iconValue.Split(TextArrays.Colon)[0];
			}

			var loopCount = DestructionExceptions.Contains(this.Name) ? 2 : 0;
			for (var i = 0; i <= loopCount; i++)
			{
				var iconName = "icon" + (i > 0 ? (i + 1).ToStringInvariant() : string.Empty);
				var destType = loopCount > 0
					? string.Create(
						CultureInfo.InvariantCulture,
						$" ({DestructionTypes[i]})")
					: string.Empty;
				var newValue = IconValueFixup(template.Find(iconName), iconValue + destType);
				template.Update(iconName, newValue, ParameterFormat.OnePerLine, true);
			}

			this.UpdateTemplate(template);
			template.Sort("titlename", "id", "id1", "id2", "id3", "id4", "id5", "id6", "id7", "id8", "id9", "id10", "line", "type", "icon", "icon2", "icon3", "desc", "desc1", "desc2", "desc3", "desc4", "desc5", "desc6", "desc7", "desc8", "desc9", "desc10", "linerank", "cost", "attrib", "casttime", "range", "radius", "duration", "channeltime", "target", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "image", "imgdesc", "nocat", "notrail");
			parser.UpdatePage();

			EsoReplacer replacer = new(page.Site);
			var newLinks = replacer.CheckNewLinks(oldPage, parser);
			if (newLinks.Count > 0)
			{
				return EsoReplacer.ConstructWarning(oldPage, parser, newLinks, "links");
			}

			var newTemplates = replacer.CheckNewTemplates(oldPage, parser);
			return newTemplates.Count > 0
				? EsoReplacer.ConstructWarning(oldPage, parser, newTemplates, "templates")
				: null;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Name} ({this.SkillLine})";
		#endregion

		#region Public Abstract Methods
		public abstract void AddData(IDataRecord row);

		public abstract bool Check();

		public abstract void SetChangeType(Skill previous);
		#endregion

		#region Public Virtual Methods
		public virtual void PostProcess()
		{
		}
		#endregion

		#region Protected Static Methods
		protected static string FormatMeters(string value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return value.OrdinalEquals("1")
				? "1 meter"
				: $"{value} meters";
		}

		protected static string FormatSeconds(string? value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return value switch
			{
				"" => string.Empty,
				"0" => "Instant",
				"1" => "1 second",
				_ => $"{value} seconds"
			};
		}

		protected static string IconValueFixup(IParameterNode? parameter, string newValue)
		{
			if (parameter != null)
			{
				var currentValue = parameter.Value.ToValue().Trim();
				if (IconNameCache.TryGetValue(currentValue, out var oldValue))
				{
					return oldValue;
				}

				IconNameCache.Add(currentValue, newValue);
			}

			return newValue;
		}

		protected static string MakeIcon(string lineName, string morphName) => lineName + "-" + morphName;

		protected static void UpdateParameter(SiteTemplateNode template, string name, string value, TitleCollection? usedList, string? skillName)
		{
			var factory = template.Factory;
			var collection = new WikiNodeCollection(factory, factory.Parse(value.Trim()));
			if (usedList != null)
			{
				EsoReplacer.ReplaceGlobal(collection);
				EsoReplacer.ReplaceEsoLinks(collection);
				EsoReplacer.ReplaceFirstLink(collection, usedList);
				if (skillName != null)
				{
					EsoReplacer.ReplaceSkillLinks(collection, skillName);
				}
			}

			template.Update(name, collection.ToRaw(), ParameterFormat.OnePerLine, true);
		}
		#endregion

		#region Protected Abstratct Methods
		protected abstract void UpdateTemplate(SiteTemplateNode template);
		#endregion
	}
}
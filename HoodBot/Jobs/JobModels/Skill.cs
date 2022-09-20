namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal abstract class Skill
	{
		#region Private Constants
		private const string TemplateName = "Online Skill Summary";
		#endregion

		#region Static Fields
		private static readonly HashSet<string> DestructionExceptions = new(StringComparer.Ordinal) { "Destructive Touch", "Impulse", "Wall of Elements" };
		private static readonly string[] DestructionTypes = { "Frost", "Shock", "Fire" };
		private static readonly string[] DoubleColonSplit = new[] { "::" };
		#endregion

		#region Constructors
		protected Skill(IDataRecord row)
		{
			this.Name = (string)row["baseName"];
			var classLine = ((string)row["skillTypeName"]).Split(DoubleColonSplit, StringSplitOptions.None);
			this.Class = classLine[0];
			this.SkillLine = classLine[1];
			var testName = this.Name;
			if (!ReplacementData.SkillNameFixes.TryGetValue(testName, out var newName))
			{
				testName = this.Name + " - " + this.SkillLine;
				ReplacementData.SkillNameFixes.TryGetValue(testName, out newName);
			}

			if (newName != null)
			{
				Debug.WriteLine("Page Name Changed: {0} => {1}", testName, newName);
			}

			this.PageName = "Online:" + (newName ?? this.Name);
		}
		#endregion

		#region Public Static Properties
		public static Regex Highlight => new(@"\|c[0-9a-fA-F]{6}|\|r", RegexOptions.None, Globals.DefaultRegexTimeout);
		#endregion

		#region Public Properties
		public bool BigChange { get; protected set; }

		public string Class { get; protected set; }

		public string Name { get; }

		public string PageName { get; }

		public IList<Rank> Ranks { get; } = new List<Rank>();

		public string SkillLine { get; protected set; }
		#endregion

		#region Private Static Properties
		private static SortedList<string, string> IconNameCache { get; } = new(StringComparer.Ordinal);
		#endregion

		#region Public Static Methods
		public static SortedList<string, string> GetIconChanges()
		{
			SortedList<string, string> iconChanges = new(IconNameCache.Count, StringComparer.Ordinal);
			foreach (var kvp in IconNameCache)
			{
				if (!string.Equals(kvp.Key, kvp.Value, StringComparison.Ordinal))
				{
					iconChanges.Add(kvp.Key, kvp.Value);
				}
			}

			return iconChanges;
		}
		#endregion

		#region Public Methods
		public string? UpdatePageText(Page page, Site site)
		{
			if (!page.Exists)
			{
				page.Text = "{{Minimal|Skill}}\n{{Online Skill Summary}}\n\n<!--\n==Notes==\n* -->\n{{Stub}}\n{{Online Skills " + this.Class + "}}";
			}

			ContextualParser oldPage = new(page);
			ContextualParser parser = new(page);
			List<SiteTemplateNode> skillSummaries = new(parser.FindSiteTemplates(TemplateName));
			if (skillSummaries.Count != 1)
			{
				return "Incorrect number of {{" + TemplateName + "}} matches on " + this.PageName;
			}

			var template = skillSummaries[0];
			template.RemoveDuplicates();
			template.Remove("update");

			var factory = new SiteNodeFactory(site);
			UpdateParameter(factory, template, "line", this.SkillLine);
			var iconValue = MakeIcon(this.SkillLine, this.Name);

			// Special cases
			if (string.Equals(iconValue, "Woodworking-Woodworking", StringComparison.Ordinal))
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
				var newValue = IconValueFixup(template.Find(iconName), iconValue + (loopCount > 0 ? FormattableString.Invariant($" ({DestructionTypes[i]})") : string.Empty));
				UpdateParameter(factory, template, iconName, newValue);
			}

			this.UpdateTemplate(factory, template);
			template.Sort("titlename", "id", "id1", "id2", "id3", "id4", "id5", "id6", "id7", "id8", "id9", "id10", "line", "type", "icon", "icon2", "icon3", "desc", "desc1", "desc2", "desc3", "desc4", "desc5", "desc6", "desc7", "desc8", "desc9", "desc10", "linerank", "cost", "attrib", "casttime", "range", "radius", "duration", "channeltime", "target", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "image", "imgdesc", "nocat", "notrail");

			EsoReplacer replacer = new(site);
			var newLinks = replacer.CheckNewLinks(oldPage, parser);
			if (newLinks.Count > 0)
			{
				return EsoReplacer.ConstructWarning(oldPage, parser, newLinks, "links");
			}

			var newTemplates = replacer.CheckNewTemplates(oldPage, parser);
			if (newTemplates.Count > 0)
			{
				return EsoReplacer.ConstructWarning(oldPage, parser, newTemplates, "templates");
			}

			parser.UpdatePage();
			return null;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Name} ({this.SkillLine})";
		#endregion

		#region Public Abstract Methods
		public abstract void AddData(IDataRecord row);

		public abstract bool Check();

		public abstract void SetBigChange(Skill prev);

		public abstract void UpdateTemplate(SiteNodeFactory factory, ITemplateNode template);
		#endregion

		#region Public Virtual Methods
		public virtual void PostProcess()
		{
		}
		#endregion

		#region Protected Static Methods
		protected static string FormatMeters(string? value) => string.Equals(value.NotNull(), "1", StringComparison.Ordinal)
			? "1 meter"
			: $"{value} meters";

		protected static string FormatSeconds(string? value) => value.NotNull() switch
		{
			"0" => "Instant",
			"1" => "1 second",
			_ => $"{value} seconds"
		};

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

		protected static void UpdateParameter(SiteNodeFactory factory, ITemplateNode template, string name, string value)
		{
			var valueNodes = factory.Parse(value.Trim());
			template.Update(name, valueNodes.ToRaw(), ParameterFormat.OnePerLine, true);
		}

		protected static void UpdateParameter(SiteNodeFactory factory, ITemplateNode template, string name, string value, TitleCollection? usedList, string? skillName)
		{
			var valueNodes = factory.Parse(value.Trim());
			if (usedList != null)
			{
				EsoReplacer.ReplaceGlobal(valueNodes);
				EsoReplacer.ReplaceEsoLinks(factory.Site, valueNodes);
				EsoReplacer.ReplaceFirstLink(valueNodes, usedList);
				if (skillName != null)
				{
					EsoReplacer.ReplaceSkillLinks(valueNodes, skillName);
				}
			}

			template.Update(name, valueNodes.ToRaw(), ParameterFormat.OnePerLine, true);
		}

		protected static void UpdateParameter(SiteNodeFactory factory, ITemplateNode template, string name, string value, bool removeCondition)
		{
			template.ThrowNull();
			if (removeCondition)
			{
				template.Remove(name);
			}
			else
			{
				UpdateParameter(factory, template, name, value);
			}
		}
		#endregion
	}
}

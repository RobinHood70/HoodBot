namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoFurnishingUpdater : TemplateJob
	{
		#region Static Fields
		private static readonly Dictionary<string, string> MaterialsLookup = new(StringComparer.OrdinalIgnoreCase)
		{
			["bast"] = "Bast",
			["clean pelt"] = "Clean Pelt",
			["heartwood"] = "Heartwood",
			["rune"] = "Mundane Rune",
			["ochre"] = "Ochre",
			["pelt"] = "Clean Pelt",
			["regulus"] = "Regulus",
			["resin"] = "Alchemical Resin",
			["wax"] = "Decorative Wax",
		};

		private static readonly Regex OtherMat = new(@"\s*(?<material>.*?)\s*\((?<count>\d+)\)", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly Dictionary<string, string> SkillsLookup = new(StringComparer.OrdinalIgnoreCase)
		{
			["alchemy"] = "Solvent Proficiency",
			["blacksmithing"] = "Metalworking",
			["clothing"] = "Tailoring",
			["enchanting"] = "Potency Improvement",
			["jewelry"] = "Engraver",
			["provisioning"] = "Recipe Improvement",
			["woodworking"] = "Woodworking (skill)",
		};
		#endregion

		#region Fields
		private readonly Dictionary<ISimpleTitle, SiteTemplateNode> dict = new(SimpleTitleEqualityComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("ESO Furnishing Updater", "|ESO")]
		public EsoFurnishingUpdater(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary { get; } = "Update missed info";

		protected override string TemplateName { get; } = "Online Furnishing Summary";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var file = File.ReadAllText(UespSite.GetBotDataFolder("Furnishing Moves.txt")).Trim();
			var lines = file.Split("~\n", StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var fields = line.Split(TextArrays.Tab);
				if (fields.Length > 0)
				{
					var template = new SiteNodeFactory(this.Site).SingleNode<SiteTemplateNode>(fields[2]);
					var title = TitleFactory.Direct(this.Site, UespNamespaces.Online, fields[1]).ToTitle();
					this.dict.Add(title, template);
				}
			}

			base.BeforeLogging();
		}

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parsedPage)
		{
			ISimpleTitle title = TitleFactory.Direct(parsedPage.Site, UespNamespaces.Online, parsedPage.Context.PageName + " (furnishing)").ToTitle();
			if (!this.dict.TryGetValue(title, out var originalTemplate))
			{
				var title2 = parsedPage.Context;
				if (!this.dict.TryGetValue(title2, out originalTemplate))
				{
					Debug.WriteLine("Missing Page:" + title.FullPageName());
					return;
				};
			}

			SortedDictionary<string, string> materials = new(StringComparer.Ordinal);
			SortedDictionary<string, string> skills = new(StringComparer.Ordinal);
			IEnumerable<(string? Key, string Value)> originalParams = originalTemplate.Parameters.ToKeyValue();

			string? stylemat = null;
			string? stylematcount = null;
			foreach (var (key, value) in originalParams)
			{
				switch (key)
				{
					case "bast":
					case "clean pelt":
					case "heartwood":
					case "ochre":
					case "pelt":
					case "regulus":
					case "resin":
					case "rune":
					case "wax":
						// Move into catchall `materials` parameter:
						materials[MaterialsLookup[key]] = value.Trim();
						break;
					case "alchemy":
					case "blacksmithing":
					case "clothing":
					case "enchanting":
					case "jewelry":
					case "provisioning":
					case "woodworking":
						// Move into catchall `skills` parameter.
						skills[SkillsLookup[key]] = value.Trim();
						break;
					case "stylemat":
						stylemat = value.Trim();
						break;
					case "stylematcount":
						stylematcount = value.Trim();
						break;
					case "othermat1":
					case "othermat2":
					case "othermat3":
					case "othermat4":
					case "othermats":
						Match otherMatch = OtherMat.Match(value.Trim());
						if (otherMatch.Success)
						{
							var otherMaterial = otherMatch.Groups["material"].Value;
							var otherCount = otherMatch.Groups["count"].Value;
							try
							{
								SiteLink siteLink = SiteLink.FromText(this.Site, otherMaterial);
								var otherKey = siteLink.Text ?? siteLink.PageName;
								materials[otherKey] = otherCount;
							}
							catch (ArgumentException)
							{
								materials[otherMaterial] = otherCount;
							}
						}

						break;
					case "achievecat":
					case "achievecat2":
					case "achievementalt":
					case "luxury":
					case "materials":
					case "nocat":
					case "questcat":
					case "recipename":
					case "species":
					case "style":
					case "trainingdummy":
						// Unused and will be discarded if nothing else changes (let me know if you want a list of values and where they're used):
						break;
					default:
						break;
				}
			}

			if (stylemat != null)
			{
				materials[stylemat] = stylematcount ?? throw new InvalidOperationException();
			}
			else if (stylematcount != null)
			{
				throw new InvalidOperationException();
			}

			if (materials.Count > 0)
			{
				StringBuilder sb = new(materials.Count * 15);
				foreach (var material in materials)
				{
					sb
						.Append(',')
						.Append(material.Key)
						.Append(',')
						.Append(material.Value);
				}

				sb.Remove(0, 1);
				if (template.Find("materials") is IParameterNode findParamMaterials)
				{
					findParamMaterials.SetValue(sb.ToString());
				}
				else
				{
					template.Add("materials", string.Join(", ", sb.ToString()));
				}
			}

			if (skills.Count > 0)
			{
				StringBuilder sb = new(skills.Count * 15);
				foreach (var skill in skills)
				{
					sb
						.Append(',')
						.Append(skill.Key)
						.Append(',')
						.Append(skill.Value);
				}

				sb.Remove(0, 1);
				if (template.Find("skills") is IParameterNode findParamSkills)
				{
					findParamSkills.SetValue(sb.ToString());
				}
				else
				{
					template.Add("skills", sb.ToString());
				}
			}
		}
		#endregion
	}
}

namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Creatures", "Starfield")]
	[SuppressMessage("Style", "IDE0001:Name can be simplified", Justification = "False hit.")]
	internal sealed class SFCreatures(JobManager jobManager) : CreateOrUpdateJob<SFCreatures.Creature>(jobManager)
	{
		#region Protected Override Properties
		protected override string? Disambiguator => "creature";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create variant redirect"; // "Create/update creature page";

		protected override bool IsValid(ContextualParser parser, Creature item) => parser.FindSiteTemplate("Creature Summary") is not null;

		protected override IDictionary<Title, Creature> LoadItems()
		{
			var retval = new Dictionary<Title, Creature>();
			var titleMap = this.GetTitleMap();
			this.ReadFile(retval, titleMap);

			return retval;
		}

		protected override void PageLoaded(ContextualParser parser, Creature item)
		{
			var cs = parser.FindSiteTemplate("Creature Summary");
			if (cs is not null)
			{
				cs.Remove("resp");
				cs.Remove("typenamesp");

				cs.RenameParameter("level", "difficulty");
				cs.RenameParameter("levels", "difficulty");

				var typeValue = cs.GetValue("type");
				if (typeValue != null && (typeValue.Length == 0 || string.Equals(typeValue, "Fauna", StringComparison.Ordinal)))
				{
					cs.Remove("type");
				}

				if (item.Variants.Count < 2)
				{
					this.UpdateLoc(cs, item);
					UpdateTemplate(cs, item.Variants[0]);
				}
				else
				{
					var planets = new SortedSet<string>(StringComparer.Ordinal);
					foreach (var variant in item.Variants)
					{
						planets.Add(variant["Planet"]);
					}

					cs.Update("planet", string.Join(", ", planets));
				}
			}

			if (parser.FindSiteTemplate("Creature Variant") is null && item.Variants.Count > 1)
			{
				AddVariants(parser, item);
			}

			foreach (var variant in item.Variants)
			{
				var resource = variant["Resource"].Split(" (", 2, StringSplitOptions.None)[0];
				var redirectText =
					$"#REDIRECT [[Starfield:{variant["Name"]}#{variant["Planet"]}]]\n" +
					$"[[Category:Starfield-Creatures-{resource}]]\n" +
					"[[Category:Redirects to Broader Subjects]]";
				var redirect = this.Site.CreatePage($"Starfield:{variant["Name"]} ({variant["Planet"]})", redirectText);
				this.Pages.Add(redirect);
			}

			parser.UpdatePage();
		}

		protected override string NewPageText(Title title, Creature item)
		{
			var sb = new StringBuilder();
			sb
				.Append("{{Creature Summary\n")
				.Append("|refid=\n")
				.Append("|baseid=\n")
				.Append("|planet=\n")
				.Append("|biomes=\n")
				.Append("|species=\n")
				.Append("|predation=\n")
				.Append("|image=\n")
				.Append("|imgdesc=\n")
				.Append("}}\n")
				.Append("{{NewLine}}\n")
				.Append("{{Stub|Fauna}}\n")
				;

			return sb.ToString();
		}
		#endregion

		#region Private Static Methods
		private static void NoNone(ITemplateNode template, string key, string value)
		{
			if (string.Equals(value, "None", StringComparison.Ordinal))
			{
				template.Update(key, value);
			}
		}

		private static void UpdateTemplate(SiteTemplateNode template, CsvRow row)
		{
			template.Update("baseid", row["FormID"]);
			//// template.Update("species", row["Race"]);
			template.Update("planet", row["Planet"]);
			template.Update("biomes", "\n* " + row["Biomes"].Replace(", ", "\n* ", StringComparison.Ordinal));
			template.Update("resource", row["Resource"].Split(" (", 2, StringSplitOptions.None)[0]);
			template.Update("temperament", row["Temperament"]);
			template.Update("harvest", row["Harvestable"]);
			template.Update("domesticable", row["Domesticable"]);
			template.Update("predation", row["Type"]);
			template.Update("difficulty", row["Difficulty"]);
			if (row["Health Mult"].Length > 0)
			{
				template.Update("health", row["Health Mult"][1..]);
			}

			template.Update("size", row["size"]);
			template.Update("diet", row["Diet"]);
			template.Update("schedule", row["Schedule"]);
			template.Update("combatstyle", row["Combat Style"]);
			NoNone(template, "abilities", row["Abilities"]);
			NoNone(template, "resistances", row["Resistances"]);
			NoNone(template, "weaknesses", row["Weaknesses"]);
			NoNone(template, "behavior", row["Behaviors"]);
		}
		#endregion

		#region Private Methods
		private static void AddVariants(ContextualParser parser, Creature item)
		{
			var newNodes = new NodeCollection(parser.Factory);
			var insertPos = parser.FindIndex<IHeaderNode>(0);
			if (insertPos == -1)
			{
				insertPos = parser.FindIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Stub"));
				if (insertPos == -1)
				{
					insertPos = parser.Count;
				}

				newNodes.AddText("\n\n");
			}

			var header = parser.Factory.HeaderNodeFromParts(2, "Variants");
			newNodes.Add(header);
			newNodes.AddText("\n");
			foreach (var variant in item.Variants)
			{
				var template = (SiteTemplateNode)parser.Factory.TemplateNodeFromParts("Creature Variant");
				UpdateTemplate(template, variant);
				newNodes.Add(template);
				newNodes.AddText("\n");
			}

			parser.InsertRange(insertPos, newNodes);
		}

		private Dictionary<string, Title> GetTitleMap()
		{
			var titleMap = new Dictionary<string, Title>(StringComparer.Ordinal);
			var existing = new PageCollection(this.Site);
			existing.GetBacklinks("Template:Creature Summary");
			foreach (var page in existing)
			{
				var parser = new ContextualParser(page);
				var template = parser.FindSiteTemplate("Creature Summary");
				if (template is not null)
				{
					var name = template.Find("titlename")?.Value.ToRaw() ?? page.Title.LabelName();
					titleMap.Add(name, page.Title);
				}
			}

			return titleMap;
		}

		private void ReadFile(Dictionary<Title, Creature> retval, Dictionary<string, Title> titleMap)
		{
			var file = new CsvFile();
			file.Load(Starfield.ModFolder + "sfcreatures_-_wip3.csv", true);
			Creature? creature = null;
			foreach (var row in file)
			{
				var name = row["Name"];
				if (!string.Equals(creature?.Variants[0]["Name"], name, StringComparison.Ordinal))
				{
					creature = new Creature([]);
					if (!titleMap.TryGetValue(name, out var title))
					{
						title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
					}

					retval.Add(title, creature);
				}

				creature!.Variants.Add(row);
			}
		}

		private void UpdateLoc(SiteTemplateNode template, Creature item)
		{
			// Remove loc if it's a link and duplicates planet
			if (template.Find("loc") is IParameterNode loc)
			{
				if (loc.Value.Count == 2 &&
				loc.Value[0] is ILinkNode linkNode)
				{
					var link = SiteLink.FromLinkNode(this.Site, linkNode);
					foreach (var row in item.Variants)
					{
						if (string.Equals(link.Text, row["Planet"], StringComparison.OrdinalIgnoreCase))
						{
							template.Remove("loc");
							break;
						}
					}
				}
			}
		}
		#endregion

		#region Internal Records
		internal sealed record Creature(List<CsvRow> Variants);
		#endregion
	}
}
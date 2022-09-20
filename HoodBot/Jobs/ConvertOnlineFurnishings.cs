namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class ConvertOnlineFurnishings : EditJob
	{
		#region Static Fields
		private static readonly string[] AntiquityNames = new[] { string.Empty, "id", "quality", "difficulty", "zone", "source", "antiquarian1", "codex1", "antiquarian2", "codex2", "antiquarian3", "codex3" };
		private static readonly string[] AntiquityFormatting = new[] { "\n  |name=", "\n  |id=", "\n  |quality=", "\n  |difficulty=", "\n  |zone=", "\n  |source=", "\n  |antiquarian1=", "\n  |codex1=", "\n  |antiquarian2=", "\n  |codex2=", "\n  |antiquarian3=", "\n  |codex3=" };
		private static readonly string[] CraftingParameters = new[] { "craft", "document", "folio", "materials", "planid", "planname", "planpricewv", "planquality", "planvendorwv", "skills" };
		private static readonly string[] NonHouseCats = new[] { "Miscellaneous", "Mounts", "Non-Combat Pets", "Services" };
		private static readonly string[] PurchaseParameters = new[] { "priceap", "pricecg", "pricecrowns", "priceet", "pricegold", "pricetv", "pricewv", "vendorap", "vendorcg", "vendorcgpreview", "vendorcity", "vendorcity2", "vendorcity3", "vendorcityap", "vendorcityother", "vendorcitytv", "vendorcrowns", "vendoret", "vendorgold", "vendorother", "vendorother2", "vendorother3", "vendortv", "vendorwv" };
		#endregion

		#region Constructors
		[JobInfo("Convert Online Furnishings", "ESO")]
		public ConvertOnlineFurnishings(JobManager jobManager)
			: base(jobManager)
		{
			// jobManager.ShowDiffs = false;
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Convert to full text";
		#endregion

		#region Protected Override Methods

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online Furnishing Summary");
		/* protected override void LoadPages() => this.Pages.GetTitles(
			"Online:Sotha Sil, The Clockwork God",
			"Online:Wild Hunt Horse (furnishing)",
			"Online:Undaunted Chest",
			"Online:Echalette (furnishing)",
			"Online:Argonian Hamper, Woven",
			"Online:Windhelm Wolfhound (furnishing)");
		*/

		protected override void PageLoaded(EditJob sender, Page page)
		{
			var originalParser = new ContextualParser(page);
			var index = originalParser.FindIndex(node => node is SiteTemplateNode templateNode &&
				templateNode.TitleValue.Namespace == MediaWikiNamespaces.Template &&
				templateNode.TitleValue.PageNameEquals("Online Furnishing Summary"));
			if (index != -1)
			{
				var template = (SiteTemplateNode)originalParser[index];
				var parser = new ContextualParser(this.Site.CreatePage(page, string.Empty))
				{
					template
				};

				/*
				template.SetTitle("User:RobinHood70/Vav");
				template.Add("name", page.PageName);
				*/
				ConvertLead(parser, template);
				ConvertSources(parser, template);
				ConvertAntiquity(parser, template);
				ConvertPurchase(parser, template);
				ConvertCrafting(parser, template);
				ConvertBooks(parser, template);
				ConvertHouses(parser, template);
				//// ConvertAchievements(parser, template);
				parser.AddText("\n");

				// The following are left in the template through all calls because they're shared. Now that we're done, we can remove them.
				template.Remove("antiquity");

				originalParser.RemoveAt(index);
				originalParser.InsertRange(index, parser);
				originalParser.UpdatePage();
			}
		}
		#endregion

		#region Private Static Methods
		/*
		private static void ConvertAchievements(ContextualParser parser, SiteTemplateNode template)
		{
			if (template.GetRaw("achievement") is string achievement && IsCollectible(template))
			{
				parser.AddParsed("\n\n" +
					"==Achievement==\n" +
					"The following [[Online:Achievements|achievement]] grants this furnishing:" +
					"{{ESO Achievements List|" + achievement + "}}");
			}
			else
			{
				parser.AddParsed("<!--Instructions: If this furnishing is granted by an achievement, enter that here.-->");
				parser.AddParsed("<!--\n" +
					"==Achievement==\n" +
					"The following [[Online:Achievements|achievement]] grants this furnishing:" +
					"{{ESO Achievements List|<Achievement Name>}}-->");
			}
		}
		*/

		private static bool IsCollectible(ITemplateNode template)
		{
			var furnLimitType = template.GetRaw("furnLimitType");
			return
				string.Equals(furnLimitType, "Collectible Furnishing", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(furnLimitType, "Special Collectible", StringComparison.OrdinalIgnoreCase);
		}

		private static void ConvertAntiquity(ContextualParser parser, SiteTemplateNode template)
		{
			if (template.GetRaw("antiquity") is string antiquity)
			{
				var sb = new StringBuilder();
				var antiquityCount = int.Parse(antiquity, parser.Page.Site.Culture);
				sb
					.Append("\n\n==Antiquity==\n{{Online Furnishing Antiquity/Start|leads=")
					.Append(antiquity);
				if (template.GetRaw("antiquityset") is string set)
				{
					sb
						.Append("|set=")
						.Append(set);
					template.Remove("antiquityset");
				}

				var sb2 = new StringBuilder();
				var multiCodex = false;
				for (var i = 1; i <= antiquityCount; i++)
				{
					multiCodex |= ParseAntiquity(sb2, template, i);
				}

				if (multiCodex)
				{
					sb.Append("|multicodex=1");
				}

				sb
					.Append("}}")
					.Append(sb2)
					.Append("\n{{Online Furnishing Antiquity/End}}");
				parser.AddParsed(sb.ToString());
				template.Remove("antiquity");
			}
			else
			{
				parser.AddParsed(
					"<!--Instructions: Fill in antiquity information. There are two styles that can be used, but only the Start/End style is used here. The shorter version may be more desirable when there's only a single item for the antiquity. See [[Template:Online Furnishing Antiquity]] for details.\n" +
					"* In the /Start template, multicodex=1 specified that there are three codexes instead of one; if there is only one, remove the parameter or leave it blank.\n" +
					"* After the set, add a /Row template for each antiquity. Remove any parameters that are empty." +
					"* Finally, add an /End to finish it off.-->");
				parser.AddParsed("<!--\n" +
					"==Antiquity==\n" +
					"{{Online Furnishing Antiquity/Start|antiquity=(count)|multicodex=|set=(set name)\n" +
					"{{Online Furnishing Antiquity/Row\n" +
					"  |name=\n" +
					"  |id=\n" +
					"  |quality=\n" +
					"  |difficulty=\n" +
					"  |zone=\n" +
					"  |source=\n" +
					"  |antiquarian1=\n" +
					"  |codex1=\n" +
					"  |antiquarian2\n" +
					"  |codex2=\n" +
					"  |antiquarian3=\n" +
					"  |codex3=\n" +
					"}}\n" +
					"{{Online Furnishing Antiquity/End}}\n\n-->");
			}
		}

		private static void ConvertBooks(ContextualParser parser, SiteTemplateNode template)
		{
			var bookList = new List<string>(36);
			for (var i = 1; i <= 36; i++)
			{
				if (template.GetRaw($"book{i}") is string book)
				{
					bookList.Add(book);
					template.Remove($"book{i}");
				}
			}

			var collection = template.GetRaw("bookcollection");
			if (bookList.Count > 0 || collection is not null)
			{
				var sb = new StringBuilder();
				sb.Append("\n\n==Books==\n{{Online Furnishing Books");
				if (collection is not null)
				{
					template.Remove("bookcollection");
					sb
						.Append("\n|collection=")
						.Append(collection);
				}

				foreach (var book in bookList)
				{
					sb
						.Append("\n|")
						.Append(book);
				}

				sb.Append("\n}}");
				parser.AddParsed(sb.ToString());
			}
			else
			{
				parser.AddParsed("<!--Instructions: Add book information here.-->");
				parser.AddParsed("<!--\n" +
					"==Books==\n" +
					"{{Online Furnishing Books\n" +
					"|bookcollection=\n" +
					"|Book 1\n" +
					"|Book 2\n" +
					"...\n" +
					"}}\n\n-->");
			}
		}

		private static void ConvertCrafting(ContextualParser parser, SiteTemplateNode template)
		{
			if (template.Find(CraftingParameters) is not null)
			{
				var sb = new StringBuilder();
				sb.Append("\n\n==Crafting==\n{{Online Furnishing Crafting");
				foreach (var name in CraftingParameters)
				{
					if (template.GetRaw(name) is string value)
					{
						sb
							.Append("\n|")
							.Append(name)
							.Append('=')
							.Append(value);
						template.Remove(name);
					}
				}

				sb.Append("\n}}");
				parser.AddParsed(sb.ToString());
			}
			else
			{
				parser.AddParsed("<!--Instructions: Use the template below to fill out the crafting details.-->");
				parser.AddParsed("<!--\n" +
					"==Crafting==\n" +
					"{{Online Furnishing Crafting\n" +
					"|craft=\n" +
					"|document=\n" +
					"|folio=\n" +
					"|materials=\n" +
					"|planid=\n" +
					"|planname=\n" +
					"|planpricewv=\n" +
					"|planquality=\n" +
					"|planvendorwv=\n" +
					"|skills=\n" +
					"}}\n\n-->");
			}
		}

		private static void ConvertHouses(ContextualParser parser, SiteTemplateNode template)
		{
			if (template.GetRaw("cat") is string cat && !NonHouseCats.Contains(cat))
			{
				parser.AddParsed("\n\n==Houses==\n{{Online Furnishing Houses}}");
			}
			else
			{
				parser.AddParsed("<!--Instructions: Add this section to the page if the main category is NOT one of: Miscellaneous, Mounts, Non-Combat Pets, or Services. There are no parameters or anything to configure; just add the section.-->");
				parser.AddParsed("<!--\n==Houses==\n{{Online Furnishing Houses}}\n\n-->");
			}
		}

		private static void ConvertLead(ContextualParser parser, SiteTemplateNode template)
		{
			// template.SetTitle("User:RobinHood70/Vav");
			// template.Parameters.Insert(0, parser.Factory.ParameterNodeFromParts("name", parser.Page.PageName + '\n'));
			if (template.Find("note") is IParameterNode note)
			{
				parser.AddText("\n");
				parser.AddRange(note.Value);
				template.Remove("note");
			}
			else
			{
				parser.AddParsed("<!--\nInstructions: Provide an initial sentence summarizing the furnishing (i.e., is it a mount/pet/etc, where the collectible comes from, how much it costs, etc).  Subsequent paragraphs provide additional information about the item, such as related NPCs, schedule, equipment, etc.  Note that quest-specific information DOES NOT belong on this page, but instead goes on the appropriate quest page.  Spoilers should be avoided.-->");
			}

			parser.AddText("\n\n");
			parser.Add(parser.Factory.TemplateNodeFromParts("NewLeft"));
		}

		private static void ConvertPurchase(ContextualParser parser, SiteTemplateNode template)
		{
			if (IsCollectible(template) && !RewardProhibited(template))
			{
				template.RenameParameter("achievement", "reward");
			}

			if (template.Find(PurchaseParameters) is not null)
			{
				var sb = new StringBuilder();
				sb.Append("\n\n==Purchase==\n{{Online Furnishing Purchase");
				if (template.GetRaw("achievement") is string achievement)
				{
					sb
						.Append("\n|achievement=")
						.Append(achievement);
				}

				if (template.Find("antiquity") is not null)
				{
					sb.Append("\n|antiquity=1");
				}

				if (template.GetRaw("quest") is string quest)
				{
					sb
						.Append("\n|quest=")
						.Append(quest);
				}

				foreach (var name in PurchaseParameters)
				{
					if (template.GetRaw(name) is string value)
					{
						sb
							.Append("\n|")
							.Append(name)
							.Append('=')
							.Append(value);
					}
				}

				sb.Append("\n}}");
				if (sb.Length > 400)
				{
					parser.AddParsed(sb.ToString());
					template.Remove("achievement");
					template.Remove("antiquity");
					template.Remove("quest");
					foreach (var name in PurchaseParameters)
					{
						template.Remove(name);
					}
				}
			}
			else
			{
				parser.AddParsed("<!--Instructions: Add vendor information here.-->");
				parser.AddParsed("<!--\n==Purchase==\n{{Online Furnishing Purchase" +
					"|achievement=" +
					"|antiquity=1" +
					"|quest=" +
					"|vendorgold=" +
					"|pricegold=" +
					"}}\n\n-->");
			}

			static bool RewardProhibited(ITemplateNode template) =>
				template.GetRaw("vendorcrowns") is string vendorCrowns &&
				(vendorCrowns.Contains("Crown Store", StringComparison.OrdinalIgnoreCase) ||
				vendorCrowns.Contains("Housing", StringComparison.OrdinalIgnoreCase));
		}

		private static void ConvertSources(ContextualParser parser, SiteTemplateNode template)
		{
			var source = template.GetRaw("source");
			var bundles = template.GetRaw("bundles");
			if (source is not null || bundles is not null)
			{
				var factory = parser.Factory;
				parser.AddText("\n\n");
				parser.Add(factory.HeaderNodeFromParts(2, "Available From"));
				if (source != null)
				{
					parser.AddText("\n* ");
					parser.AddParsed(source);
					template.Remove("source");
				}

				if (bundles != null)
				{
					var split = bundles.Split(TextArrays.Comma);
					foreach (var bundle in split)
					{
						parser.AddText("\n* ");
						var pageName = bundle.Trim();
						if (pageName.Contains("[[", StringComparison.Ordinal))
						{
							parser.AddText(pageName);
						}
						else
						{
							parser.Add(factory.LinkNodeFromWikiText($"[[Online:{pageName}|{pageName}]]"));
						}
					}

					template.Remove("bundles");
				}
			}
			else
			{
				parser.AddParsed("<!--Instructions: List the sources from which you can get the item.-->");
				parser.AddParsed("<!--\n" +
					"==Available From==\n" +
					"* Loot - Chests in [[Online:Coldharbour|Coldharbour]]\n" +
					"* [[Furnishing Pack: Forge-Lord's Great Works|]]\n\n-->");
			}
		}

		private static bool ParseAntiquity(StringBuilder sb, SiteTemplateNode template, int i)
		{
			var multiCodex = false;
			var name = "lead" + (i == 1 ? string.Empty : i.ToStringInvariant());
			var sb2 = new StringBuilder();
			for (var index = 0; index < AntiquityNames.Length; index++)
			{
				var subName = AntiquityNames[index];
				var fullName = name + subName;
				if (template.GetRaw(fullName) is string value)
				{
					var formatted = AntiquityFormatting[index];
					if (value.Length > 0)
					{
						multiCodex |= index >= 8;
						sb2
							.Append(formatted)
							.Append(value);
					}

					template.Remove(fullName);
				}
			}

			if (sb2.Length > 0)
			{
				sb
					.Append("\n{{Online Furnishing Antiquity/Row")
					.Append(sb2)
					.Append("\n}}");
			}

			return multiCodex;
		}
		#endregion
	}
}

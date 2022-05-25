namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal enum ItemType
	{
		Container = 18,
		Recipes = 29,
		Furnishing = 61,
	}

	internal sealed class EsoFurnishingUpdater : TemplateJob
	{
		#region Static Fields
		private static readonly string CollectiblesQuery = $"SELECT convert(cast(convert(description using latin1) as binary) using utf8) description, furnCategory, furnLimitType, furnSubCategory, id itemId, itemLink resultitemLink, name, nickname FROM collectibles WHERE furnCategory != ''";
		private static readonly string MinedItemsQuery = $"SELECT abilityDesc, bindType, convert(cast(convert(description using latin1) as binary) using utf8) description, furnCategory, furnLimitType, itemId, name, quality, resultitemLink, tags, type FROM uesp_esolog.minedItemSummary WHERE type IN({(int)ItemType.Container}, {(int)ItemType.Recipes}, {(int)ItemType.Furnishing})";

		private static readonly HashSet<string> AliveCats = new(StringComparer.Ordinal)
		{
			"Amory Assitants",
			"Banking Assistants",
			"Companions",
			"Creatures",
			"Deconstruction Assistants",
			"Houseguests",
			"Merchant Assistants",
			"Mounts",
			"Non-Combat Pets",
			"Statues",
		};
		#endregion

		#region Fields
		private readonly Dictionary<long, Furnishing> furnishings = new();
		private readonly List<string> fileMessages = new();
		private readonly List<string> pageMessages = new();
		//// private readonly Dictionary<Title, Furnishing> furnishingDictionary = new(SimpleTitleComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("ESO Furnishing Updater", "ESO Update")]
		public EsoFurnishingUpdater(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary { get; } = "Update info from ESO database";

		protected override string TemplateName { get; } = "Online Furnishing Summary";
		#endregion

		#region Protected Override Methods
		protected override void AfterLoadPages()
		{
			if (this.pageMessages.Count == 0 && this.fileMessages.Count == 0)
			{
				return;
			}

			this.WriteLine("__FORCETOC__");
			if (this.pageMessages.Count > 0)
			{
				this.WriteLine("== Online Page Name Issues ==");
				this.pageMessages.Sort(StringComparer.Ordinal);
				foreach (var message in this.pageMessages)
				{
					this.WriteLine(message);
					this.WriteLine(string.Empty);
				}
			}

			if (this.fileMessages.Count > 0)
			{
				this.WriteLine("== File Page Name Issues ==");
				this.fileMessages.Sort(StringComparer.Ordinal);
				foreach (var message in this.fileMessages)
				{
					this.WriteLine(message);
					this.WriteLine(string.Empty);
				}
			}
		}

		protected override void BeforeLoadPages()
		{
			/*
			TitleCollection furnishingFiles = new(this.Site);
			furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-furnishing-");
			furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-item-furnishing-");
			*/

			foreach (var furnishing in Database.RunQuery(EsoLog.Connection, MinedItemsQuery, (IDataRecord record) => new Furnishing(record, this.Site, false)))
			{
				this.furnishings.Add(furnishing.Id, furnishing);
			}

			foreach (var furnishing in Database.RunQuery(EsoLog.Connection, CollectiblesQuery, (IDataRecord record) => new Furnishing(record, this.Site, true)))
			{
				this.furnishings.Add(furnishing.Id, furnishing);
			}
		}

		protected override void FillPage(Page page) => Debug.WriteLine($"{page.AsLink()} doesn't exist and will be created.");

		protected override void LoadPages()
		{
			var titles = new TitleCollection(this.Site,
				"Online:Statue, Kaalgrontiid's Ascent (aeonfire)",
				"Online:Statue, Kaalgrontiid's Ascent (flame)",
				"Online:Statue, Kaalgrontiid's Ascent (frost)",
				"Online:Statue, Kaalgrontiid's Ascent (shock)",
				"Online:Statue, Kaalgrontiid's Ascent",
				"Online:Statue, Prince of Ambition",
				"Online:Witches Festival Scarecrow",
				"Online:Jellyfish Bloom, Heliotrope",
				"Online:Minnow School",
				"Online:Soul-Shriven, Armored",
				"Online:Soul-Shriven, Robed",
				"Online:Bubbles of Aeration",
				"Online:Steam of Repose",
				"Online:Transliminal Rupture",
				"Online:Fogs of the Hag Fen",
				"Online:Mists of the Hag Fen",
				"Online:Nightmare Stick Horse (furnishing)",
				"Online:Echalette (furnishing)"
				);

			this.Pages.GetTitles(titles);

		}

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			parser.ThrowNull();
			var labelName = parser.Page.LabelName();
			if (string.Equals(template.GetValue("name"), labelName, StringComparison.Ordinal))
			{
				template.Remove("name");
			}

			this.GenericTemplateFixes(template);
			this.FurnishingFixes(template, parser.Page);
		}
		#endregion

		#region Private Static Methods
		private static (Title OldTitle, Title NewTitle) CheckImageName(Page page, SiteTemplateNode template, bool collectible)
		{
			var name = template.GetValue("name") ?? page.LabelName();
			var fileName = template.GetValue("image") ?? Furnishing.ImageName(name, collectible);

			var nameFix = name.Replace(':', ',');
			var fileTitle = TitleFactory.FromUnvalidated(page.Site[MediaWikiNamespaces.File], fileName);
			var fileNameFix = TitleFactory.FromUnvalidated(page.Site[MediaWikiNamespaces.File], Furnishing.ImageName(nameFix, collectible));
			return (fileTitle, fileNameFix);
		}
		#endregion

		#region Private Methods
		private static void FixBehavior(SiteTemplateNode template)
		{
			if (template.Find("behavior") is IParameterNode behavior)
			{
				var list = new List<string>(behavior.Value.ToRaw().Split(TextArrays.Comma));
				for (var i = list.Count - 1; i >= 0; i--)
				{
					list[i] = list[i].Trim();
					if (list[i].Length == 0)
					{
						list.RemoveAt(i);
					}
					else if (list[i].StartsWith("Light ", StringComparison.OrdinalIgnoreCase))
					{
						list[i] = "Light";
					}
				}

				behavior.SetValue(string.Join(",", list), ParameterFormat.OnePerLine);
			}
		}

		private void FixBundles(SiteTemplateNode template)
		{
			if (template.Find("bundles") is IParameterNode bundles)
			{
				var value = bundles.Value;
				var factory = template.Factory;
				for (var i = 0; i < value.Count; i++)
				{
					if (value is ILinkNode link)
					{
						var siteLink = SiteLink.FromLinkNode(this.Site, link);
						value.RemoveAt(i);
						if (siteLink.Text is string text)
						{
							value.Insert(i, factory.TextNode(text));
						}
					}
				}
			}
		}

		private void FixList(SiteTemplateNode template, string parameterName)
		{
			var plural = parameterName + "s";
			if (template.Find(plural, parameterName) is IParameterNode param)
			{
				param.SetName(plural);
				var curValue = param.Value;
				var curText = curValue.ToRaw();
				var splitOn = curText.Contains('~', StringComparison.Ordinal) ? '~' : ',';
				var split = curText.Split(splitOn, StringSplitOptions.None);
				var list = new List<(string Name, int Value)>(split.Length / 2);
				for (var i = 0; i < split.Length; i += 2)
				{
					split[i + 1] = split[i + 1]
						.Replace(" ", string.Empty, StringComparison.Ordinal)
						.Replace("(", string.Empty, StringComparison.Ordinal)
						.Replace(")", string.Empty, StringComparison.Ordinal);
					var intValue = split[i + 1].Length == 0 ? 1 : int.Parse(split[i + 1], this.Site.Culture);
					list.Add((split[i], intValue));
				}

				if (string.Equals(parameterName, "material", StringComparison.Ordinal))
				{
					list.Sort((item1, item2) =>
						item2.Value.CompareTo(item1.Value) is int result && result == 0
							? string.Compare(item1.Name, item2.Name, false, this.Site.Culture)
							: result);
				}

				var sb = new StringBuilder(list.Count * 10);
				foreach (var (name, value) in list)
				{
					sb
						.Append(name)
						.Append('~')
						.Append(value.ToStringInvariant())
						.Append('~');
				}

				if (sb.Length > 0)
				{
					sb.Remove(sb.Length - 1, 1);
				}

				param.SetValue(sb.ToString(), ParameterFormat.OnePerLine);
			}
		}

		private void FurnishingFixes(SiteTemplateNode template, Page? page)
		{
			page.ThrowNull();
			if (template.GetValue("id") is not string idText || string.IsNullOrEmpty(idText) || !int.TryParse(idText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, page.Site.Culture, out var id))
			{
				Debug.WriteLine($"Furnishing ID on {page.AsLink()} is missing or nonsensical.");
				return;
			}

			if (!this.furnishings.TryGetValue(id, out var furnishing))
			{
				Debug.WriteLine($"Furnishing ID {id} not found on page {page.AsLink()}.");
				return;
			}

			if (!string.Equals(page.LabelName(), furnishing.Title.LabelName(), StringComparison.Ordinal))
			{
				this.pageMessages.Add($"[[{page.FullPageName}|{page.LabelName()}]] ''should be''<br>\n" +
				  $"{furnishing.Title.PageName}");
				if (!page.PageName.Contains(':', StringComparison.Ordinal) && furnishing.Title.PageName.Contains(':', StringComparison.Ordinal) && string.Equals(page.PageName.Replace(',', ':'), furnishing.Title.PageName, StringComparison.Ordinal))
				{
					Debug.WriteLine($"Page Replace: {page.FullPageName}\t{furnishing.Title}");
				}
			}

			var (oldTitle, newTitle) = CheckImageName(page, template, furnishing.Collectible);
			{
				if (!string.Equals(oldTitle.LabelName(), newTitle.LabelName(), StringComparison.Ordinal))
				{
					this.fileMessages.Add($"{oldTitle.AsLink(LinkFormat.LabelName)} on {page.AsLink(LinkFormat.LabelName)} ''should be''<br>\n{newTitle.PageName}");

					var noItem1 = oldTitle.PageName.Replace("-item-", "-", StringComparison.Ordinal);
					var noItem2 = newTitle.PageName.Replace("-item-", "-", StringComparison.Ordinal);
					if ((oldTitle.PageName.Contains("-item-", StringComparison.Ordinal) ||
						newTitle.PageName.Contains("-item-", StringComparison.Ordinal)) &&
						string.Equals(noItem1, noItem2, StringComparison.Ordinal))
					{
						Debug.WriteLine($"File Replace Needed:\n  {oldTitle.FullPageName} with\n  {newTitle.FullPageName}");
					}
				}
			}

			template.Update("titlename", furnishing.TitleName, ParameterFormat.NoChange, true);
			if (furnishing.Collectible)
			{
				template.Update("nickname", furnishing.NickName, ParameterFormat.NoChange, true);
			}

			var imageName = Furnishing.ImageName(page.LabelName(), furnishing.Collectible);
			if (string.Equals(template.GetValue("image"), imageName, StringComparison.Ordinal))
			{
				template.Remove("image");
			}

			template.Update("quality", furnishing.Quality, ParameterFormat.OnePerLine, true);
			template.Update("desc", furnishing.Description, ParameterFormat.OnePerLine, false);
			template.Update("cat", furnishing.FurnishingCategory, ParameterFormat.OnePerLine, true);
			template.Update("subcat", furnishing.FurnishingSubcategory, ParameterFormat.OnePerLine, true);
			if (furnishing.Behavior?.Length > 0)
			{
				if (template.GetValue("behavior")?.Trim(',').Replace(",,", ",", StringComparison.Ordinal).Length == 0)
				{
					template.Remove("behavior");
				}
				else
				{
					template.AddIfNotExists("behavior", furnishing.Behavior, ParameterFormat.OnePerLine);
				}
			}

			if (furnishing.Materials.Count > 0)
			{
				template.Update("materials", string.Join("~", furnishing.Materials), ParameterFormat.OnePerLine, true);
			}

			if (furnishing.Skills.Count > 0)
			{
				template.Update("skills", string.Join("~", furnishing.Skills), ParameterFormat.OnePerLine, true);
			}

			if (string.IsNullOrEmpty(furnishing.BindType))
			{
				template.Remove("bindtype");
			}
			else
			{
				template.Update("bindtype", furnishing.BindType);
			}

			if (furnishing.FurnishingLimitType.Length > 0)
			{
				template.Update("furnLimitType", furnishing.FurnishingLimitType);
			}
		}

		private void GenericTemplateFixes(SiteTemplateNode template)
		{
			template.Remove("1");
			template.Remove("animated");
			template.Remove("audible");
			template.Remove("collectible");
			template.Remove("creature");
			template.Remove("houses");
			template.Remove("interactable");
			template.Remove("light");
			template.Remove("lightcolour");
			template.Remove("luxury");
			template.Remove("master");
			template.Remove("readable");
			template.Remove("sittable");
			template.Remove("visualfx");

			template.RenameParameter("other", "source");
			template.RenameParameter("recipeid", "planid");
			template.RenameParameter("recipename", "planname");
			template.RenameParameter("recipequality", "planquality");
			template.RenameParameter("style", "theme");
			template.RenameParameter("tags", "behavior");
			template.RenameParameter("type", "furnLimitType");

			FixBehavior(template);
			this.FixBundles(template);
			this.FixList(template, "material");
			this.FixList(template, "skill");
		}
		#endregion

		#region Private Classes
		private sealed class Furnishing
		{
			#region Static Fields
			private static readonly Regex IngredientsFinder = new(@"\|cffffffINGREDIENTS\|r\n(?<ingredients>.+)$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
			private static readonly Regex SizeFinder = new(@"This is a (?<size>\w+) house item.", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			private static readonly HashSet<string> AllSkills = new(StringComparer.Ordinal)
			{
				"Engraver",
				"Metalworking",
				"Potency Improvement",
				"Provisioning",
				"Recipe Improvement",
				"Solvent Proficiency",
				"Tailoring",
				"Woodworking",
			};
			#endregion

			#region Constructors
			public Furnishing(IDataRecord record, Site site, bool collectible)
			{
				this.Collectible = collectible;
				this.Id = collectible ? (long)record["itemId"] : (int)record["itemId"];
				var titleName = (string)record["name"];
				titleName = titleName.TrimEnd(',');
				titleName = site.SanitizePageName(titleName);
				this.Title = TitleFactory.FromUnvalidated(site[UespNamespaces.Online], titleName);
				if (!this.Title.PageNameEquals(titleName))
				{
					this.TitleName = titleName;
				}

				var desc = (string)record["description"];
				var sizeMatch = SizeFinder.Match(desc);
				this.Size = sizeMatch.Success ? sizeMatch.Groups["size"].Value : null;
				desc = desc
					.Replace(" |cFFFFFF", "\n:", StringComparison.Ordinal)
					.Replace("|r", string.Empty, StringComparison.Ordinal);
				this.Description = sizeMatch.Index == 0 && sizeMatch.Length == desc.Length ? null : desc;
				var furnCategory = (string)record["furnCategory"];
				var furnishingLimitType = collectible ? (sbyte)record["furnLimitType"] : (int)record["furnLimitType"];
				if (collectible)
				{
					this.FurnishingCategory = furnCategory;
					this.FurnishingSubcategory = (string)record["furnSubCategory"];
					this.NickName = (string)record["nickname"];
				}
				else
				{
					var bindType = (int)record["bindType"];
					this.BindType = bindType switch
					{
						-1 => string.Empty,
						0 => string.Empty,
						1 => "Bind on Pickup",
						2 => "Bind on Equip",
						3 => "Backpack Bind on Pickup",
						_ => throw new InvalidOperationException()
					};

					if (!string.IsNullOrEmpty(furnCategory))
					{
						var furnSplit = furnCategory.Split(TextArrays.Colon, 2);
						if (furnSplit.Length > 0)
						{
							this.FurnishingCategory = furnSplit[0];
							this.FurnishingSubcategory = furnSplit[1]
								.Split(TextArrays.Parentheses)[0]
								.TrimEnd();
						}
					}

					var quality = (string)record["quality"];
					this.Quality = int.TryParse(quality, NumberStyles.Integer, site.Culture, out var qualityNum)
						? "nfsel".Substring(qualityNum - 1, 1)
						: quality;
					this.Behavior = ((string)record["tags"])
						.Replace(",,", ",", StringComparison.Ordinal)
						.Trim(',');
					this.Type = (ItemType)record["type"];
					var abilityDesc = (string)record["abilityDesc"];
					var ingrMatch = IngredientsFinder.Match(abilityDesc);
					if (ingrMatch.Success)
					{
						var ingredientList = ingrMatch.Groups["ingredients"].Value;
						var entries = ingredientList.Split("), ", StringSplitOptions.None);
						foreach (var entry in entries)
						{
							var ingSplit = entry.Split(" (", 2, StringSplitOptions.None);
							var count = ingSplit[1];
							var ingredient = ingSplit[0];
							var addAs = $"{ingredient} ({count})";
							if (AllSkills.Contains(ingredient))
							{
								this.Skills.Add(addAs);
							}
							else
							{
								this.Materials.Add(addAs);
							}
						}
					}
				}

				if (furnishingLimitType == -1)
				{
					furnishingLimitType =
						(AliveCats.Contains(this.FurnishingCategory!) || AliveCats.Contains(this.FurnishingSubcategory!))
							? this.Collectible
								? 3
								: 1
							: this.Collectible
								? 2
								: 0;
				}

				this.FurnishingLimitType = furnishingLimitType switch
				{
					0 => "Traditional Furnishings",
					1 => "Special Furnishings",
					2 => "Collectible Furnishings",
					3 => "Special Collectibles",
					_ => throw new InvalidOperationException()
				};

				var itemLink = (string)record["resultitemLink"];
				this.ResultItemLink = EsoLog.ExtractItemId(itemLink);
			}
			#endregion

			#region Public Properties
			public string? Behavior { get; }

			public string? BindType { get; }

			public bool Collectible { get; }

			public string? Description { get; }

			public string FurnishingLimitType { get; }

			public string? FurnishingCategory { get; }

			public string? FurnishingSubcategory { get; }

			public long Id { get; }

			public SortedSet<string> Materials { get; } = new(StringComparer.Ordinal);

			public string? NickName { get; }

			public string? Quality { get; }

			public string? ResultItemLink { get; }

			public string? Size { get; }

			public SortedSet<string> Skills { get; } = new(StringComparer.Ordinal);

			public Title Title { get; }

			public string? TitleName { get; }

			public ItemType Type { get; }
			#endregion

			#region Public Static Methods
			public static string ImageName(string itemName, bool collectible) => $"ON-{(collectible ? string.Empty : "item-")}furnishing-{itemName.Replace(':', ',')}.jpg";
			#endregion

			#region Public Override Methods
			public override string ToString() => $"({this.Id}) {this.TitleName}";
			#endregion
		}
		#endregion
	}
}

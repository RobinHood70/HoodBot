namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Globalization;
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
		Recipes = 29,
		Furnishing = 61,
	}

	internal sealed class EsoFurnishingUpdater : ParsedPageJob
	{
		#region Static Fields
		private static readonly string CollectiblesQuery = $"SELECT id itemId, name, nickname, convert(cast(convert(description using latin1) as binary) using utf8) description, itemLink resultitemLink, furnCategory, furnSubCategory FROM collectibles WHERE furnCategory != ''";
		private static readonly string MinedItemsQuery = $"SELECT itemId, name, furnCategory, quality, tags, type, convert(cast(convert(description using latin1) as binary) using utf8) description, abilityDesc, resultitemLink FROM uesp_esolog.minedItemSummary WHERE type IN({(int)ItemType.Recipes}, {(int)ItemType.Furnishing})";
		#endregion

		#region Fields
		private readonly Dictionary<long, Furnishing> furnishings = new();
		private readonly List<string> fileMessages = new();
		private readonly List<string> pageMessages = new();
		//// private readonly Dictionary<Title, Furnishing> furnishingDictionary = new(SimpleTitleComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("ESO Furnishing Updater", "ESO")]
		public EsoFurnishingUpdater(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Static Properties
		public static bool RemoveColons { get; set; } // = true;
		#endregion

		#region Protected Override Properties
		protected override string EditSummary { get; } = "Update info from ESO database";
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

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online Furnishing Summary");

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			var page = parsedPage.Page;
			if (parsedPage.FindSiteTemplate("Online Furnishing Summary") is not SiteTemplateNode template ||
				template.GetValue("id") is not string idText ||
				string.IsNullOrEmpty(idText) ||
				!int.TryParse(idText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, page.Site.Culture, out var id) ||
				!this.furnishings.TryGetValue(id, out var furnishing))
			{
				return;
			}

			template.Remove("1");
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
						Debug.WriteLine($"File Replace: {oldTitle.FullPageName}\t{newTitle.FullPageName}");
					}
				}
			}

			var labelName = page.LabelName();
			if (string.Equals(template.GetValue("name"), labelName, StringComparison.Ordinal))
			{
				template.Remove("name");
			}

			template.Update("titlename", furnishing.TitleName, ParameterFormat.NoChange, true);
			if (furnishing.Collectible)
			{
				template.Update("nickname", furnishing.NickName, ParameterFormat.NoChange, true);
			}

			var imageName = Furnishing.ImageName(labelName, furnishing.Collectible);
			if (string.Equals(template.GetValue("image"), imageName, StringComparison.Ordinal))
			{
				template.Remove("image");
			}

			template.Update("quality", furnishing.Quality, ParameterFormat.OnePerLine, true);
			template.Update("desc", furnishing.Description, ParameterFormat.OnePerLine, false);
			template.Update("cat", furnishing.FurnitureCategory, ParameterFormat.OnePerLine, true);
			template.Update("subcat", furnishing.FurnitureSubcategory, ParameterFormat.OnePerLine, true);
			if (furnishing.Tags?.Length > 0)
			{
				if (template.GetValue("tags")?.Trim(',').Replace(",,", ",", StringComparison.Ordinal).Length == 0)
				{
					template.Remove("tags");
				}
				else
				{
					template.AddIfNotExists("tags", furnishing.Tags, ParameterFormat.OnePerLine);
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

			if (string.IsNullOrEmpty(template.GetValue("collectible")) == furnishing.Collectible)
			{
				this.StatusWriteLine("Collectible value changing for " + furnishing.Title.PageName);
				template.Update("collectible", furnishing.Collectible ? "y" : string.Empty, ParameterFormat.OnePerLine, true);
			}
		}
		#endregion

		#region Private Methods
		private static (Title OldTitle, Title NewTitle) CheckImageName(Page page, SiteTemplateNode template, bool collectible)
		{
			var name = template.GetValue("name") ?? page.LabelName();
			var fileName = template.GetValue("image") ?? Furnishing.ImageName(name, collectible);

			var nameFix = name.Replace(":", RemoveColons ? string.Empty : ",", StringComparison.Ordinal);
			var fileTitle = TitleFactory.FromUnvalidated(page.Site[MediaWikiNamespaces.File], fileName);
			var fileNameFix = TitleFactory.FromUnvalidated(page.Site[MediaWikiNamespaces.File], Furnishing.ImageName(nameFix, collectible));
			return (fileTitle, fileNameFix);
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
				if (collectible)
				{
					this.FurnitureCategory = furnCategory;
					this.FurnitureSubcategory = (string)record["furnSubCategory"];
					this.NickName = (string)record["nickname"];
				}
				else
				{
					if (!string.IsNullOrEmpty(furnCategory))
					{
						var furnSplit = furnCategory.Split(TextArrays.Colon, 2);
						if (furnSplit.Length > 0)
						{
							this.FurnitureCategory = furnSplit[0];
							this.FurnitureSubcategory = furnSplit[1]
								.Split(TextArrays.Parentheses)[0]
								.TrimEnd();
						}
					}

					var quality = (string)record["quality"];
					this.Quality = int.TryParse(quality, NumberStyles.Integer, site.Culture, out var qualityNum)
						? "nfsel".Substring(qualityNum - 1, 1)
						: quality;
					this.Tags = ((string)record["tags"])
						.Trim(',')
						.Replace(",,", ",", StringComparison.Ordinal);
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

				var itemLink = (string)record["resultitemLink"];
				this.ResultItemLink = EsoLog.ExtractItemId(itemLink);
			}
			#endregion

			#region Public Properties
			public bool Collectible { get; }

			public string? Description { get; }

			public string? FurnitureCategory { get; }

			public string? FurnitureSubcategory { get; }

			public long Id { get; }

			public SortedSet<string> Materials { get; } = new(StringComparer.Ordinal);

			public string? NickName { get; }

			public string? Quality { get; }

			public string? ResultItemLink { get; }

			public string? Size { get; }

			public SortedSet<string> Skills { get; } = new(StringComparer.Ordinal);

			public string? Tags { get; }

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

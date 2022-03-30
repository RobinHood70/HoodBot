namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
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
		#region Private Static Fields
		private static readonly string Query = $"SELECT itemId, name, furnCategory, quality, tags, description, abilityDesc, resultitemLink FROM uesp_esolog.minedItemSummary WHERE type IN({(int)ItemType.Recipes}, {(int)ItemType.Furnishing});";
		#endregion

		#region Fields
		private readonly Dictionary<int, Furnishing> furnishings = new();
		private readonly List<string> fileMessages = new();
		private readonly List<string> pageMessages = new();
		//// private readonly Dictionary<Title, Furnishing> furnishingDictionary = new(SimpleTitleComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("ESO Furnishing Updater", "|ESO")]
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
			this.WriteLine("__FORCETOC__");
			this.WriteLine("== Online Page Name Issues ==");
			this.pageMessages.Sort(StringComparer.Ordinal);
			foreach (var message in this.pageMessages)
			{
				this.WriteLine(message);
				this.WriteLine();
			}

			this.WriteLine("== File Page Name Issues ==");
			this.fileMessages.Sort(StringComparer.Ordinal);
			foreach (var message in this.fileMessages)
			{
				this.WriteLine(message);
				this.WriteLine();
			}
		}

		protected override void BeforeLoadPages()
		{
			/*
			TitleCollection furnishingFiles = new(this.Site);
			furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-furnishing-");
			furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-item-furnishing-");
			*/

			foreach (var furnishing in Database.RunQuery(EsoLog.Connection, Query, (IDataRecord record) => new Furnishing(record, this.Site)))
			{
				this.furnishings.Add(furnishing.Id, furnishing);
			}
		}

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online Furnishing Summary");

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			var page = parsedPage.Page;
			if (parsedPage.FindSiteTemplate("Online Furnishing Summary") is SiteTemplateNode template)
			{
				if (template.GetValue("id") is string idText &&
					!string.IsNullOrEmpty(idText) &&
					int.TryParse(idText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, page.Site.Culture, out var id))
				{
					var collectible = string.IsNullOrEmpty(template.GetValue("collectible"));
					if (collectible && this.furnishings.TryGetValue(id, out var furnishing))
					{
						if (this.DoPageChecks(page, id) is string pageMessage)
						{
							this.pageMessages.Add(pageMessage);
						}

						if (!page.PageNameEquals(furnishing.PageName))
						{
							this.pageMessages.Add($"Page name mismatch: {furnishing.PageName} != {page.PageName}");
						}

						if (furnishing.TitleName != null)
						{
							template.Update("titlename", furnishing.TitleName, ParameterFormat.NoChange);
						}

						template.Update("cat", furnishing.FurnitureCategory, ParameterFormat.OnePerLine);
						template.Update("subcat", furnishing.FurnitureSubcategory, ParameterFormat.OnePerLine);
						template.Update("desc", furnishing.Description, ParameterFormat.OnePerLine);
						template.Update("quality", furnishing.Quality, ParameterFormat.OnePerLine);
						template.Update("tags", furnishing.Tags, ParameterFormat.OnePerLine);
						template.Update("skillsIngredients", furnishing.AbilityDescription, ParameterFormat.OnePerLine);
						//// template.AddOrChange("result", )
						/*
									public string AbilityDescription { get; }
									public int ResultItemLink { get; }
									public string Tags { get; }
						*/
					}
					else if (!collectible)
					{
					}
					else
					{
						this.WriteLine($"{id} ({page.PageName}) not found.");
					}
				}

				if (this.DoImageChecks(template, page) is string fileMessage)
				{
					this.fileMessages.Add(fileMessage);
				}
			}
		}
		#endregion

		#region Private Methods
		private string? DoImageChecks(SiteTemplateNode template, Page page)
		{
			var collectible = template.GetValue("collectible")?.Length > 0;
			var prefix = "ON-" + (collectible ? string.Empty : "item-") + "furnishing-";
			var name = template.GetValue("name") ?? page.LabelName();
			var fileName = template.GetValue("image") ?? (prefix + name + ".jpg");
			var fileNameFix = prefix + name.Replace(":", RemoveColons ? string.Empty : ",", StringComparison.Ordinal) + ".jpg";
			var fileTitle = CreateTitle.FromUnvalidated(this.Site, MediaWikiNamespaces.File, fileName);
			var fixMatch = string.Equals(fileName, fileNameFix, StringComparison.Ordinal);
			return fixMatch
				? null
				: $":{fileTitle.AsLink(LinkFormat.LabelName)} on {page.AsLink(LinkFormat.LabelName)} ''should be''\n" +
					$":{fileNameFix}";
		}

		private string? DoPageChecks(Page page, int id) =>
			this.furnishings.TryGetValue(id, out var furnishing) &&
			!string.Equals(furnishing.PageName, page.LabelName(), StringComparison.Ordinal)
				? $":[[{page.FullPageName}|{page.LabelName()}]] ''should be''\n" +
					$":{furnishing.PageName}"
				: null;
		#endregion

		#region Private Classes
		private sealed class Furnishing
		{
			#region Constructors
			public Furnishing(IDataRecord record, Site site)
			{
				this.Id = (int)record["itemId"];
				var titleName = (string)record["name"];
				this.PageName = site.SanitizePageName(titleName);
				if (!string.Equals(this.PageName, titleName, StringComparison.Ordinal))
				{
					this.TitleName = titleName;
				}

				var furnCategory = (string)record["furnCategory"];
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

				this.Quality = (string)record["quality"];
				this.Tags = (string)record["tags"];
				this.Description = (string)record["description"];
				this.AbilityDescription = (string)record["abilityDesc"];
				var itemLinkText = (string)record["resultitemLink"];
				var itemLinkOffset1 = itemLinkText.IndexOf(":item:", StringComparison.Ordinal) + 6;
				if (itemLinkOffset1 != 5 &&
					itemLinkText.IndexOf(':', itemLinkOffset1) is var itemLinkOffset2 &&
					itemLinkOffset2 != -1 &&
					int.TryParse(itemLinkText[itemLinkOffset1..itemLinkOffset2], NumberStyles.Integer, site.Culture, out var itemLink))
				{
					this.ResultItemLink = itemLink;
				}
			}
			#endregion

			#region Public Properties
			public string AbilityDescription { get; }

			public string Description { get; }

			public string? FurnitureCategory { get; }

			public string? FurnitureSubcategory { get; }

			public int Id { get; }

			public string PageName { get; }

			public string Quality { get; }

			public int ResultItemLink { get; }

			public string Tags { get; }

			public string? TitleName { get; }
			#endregion

			#region Public Override Methods
			public override string ToString() => $"({this.Id}) {this.TitleName}";
			#endregion
		}
		#endregion
	}
}

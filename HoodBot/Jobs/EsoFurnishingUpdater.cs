namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
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
		private static readonly string Query = "SELECT * FROM uesp_esolog.minedItemSummary WHERE type=" + (int)ItemType.Furnishing;
		#endregion

		#region Fields
		private readonly Dictionary<int, Furnishing> furnishings = new();
		private readonly List<string> fileMessages = new();
		private readonly List<string> pageMessages = new();
		#endregion

		/*
		#region Fields
		private readonly Dictionary<Title, Furnishing> furnishingDictionary = new(SimpleTitleComparer.Instance);
		#endregion
		*/

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

			foreach (var furnishing in Database.RunQuery(EsoLog.Connection, Query, Furnishing.Create))
			{
				this.furnishings.Add(furnishing.Id, furnishing);
			}
		}

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online Furnishing Summary");

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			Page title = (Page)parsedPage.Title;
			if (parsedPage.FindSiteTemplate("Online Furnishing Summary") is SiteTemplateNode template)
			{
				if (this.DoPageChecks(template, title) is string pageMessage)
				{
					this.pageMessages.Add(pageMessage);
				}

				if (this.DoImageChecks(template, title) is string fileMessage)
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
			TitleFactory fileTitle = TitleFactory.Direct(this.Site, MediaWikiNamespaces.File, fileName);
			var fixMatch = string.Equals(fileName, fileNameFix, StringComparison.Ordinal);
			return fixMatch
				? null
				: $":{fileTitle.AsLink(true)} on {page.AsLink(true)} ''should be''\n" +
					$":{fileNameFix}";
		}

		private string? DoPageChecks(SiteTemplateNode template, Page page)
		{
			if (template.GetValue("id") is string idText &&
				!string.IsNullOrEmpty(idText) &&
				int.TryParse(idText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, page.Site.Culture, out var id))
			{
				if (this.furnishings.TryGetValue(id, out var furnishing))
				{
					if (!string.Equals(furnishing.Name, page.LabelName(), StringComparison.Ordinal))
					{
						return
							$":[[{page.FullPageName}|{page.LabelName()}]] ''should be''\n" +
							$":{furnishing.Name}";
					}
				}
			}

			return null;
		}
		#endregion

		#region Private Classes
		private sealed class Furnishing
		{
			#region Constructors
			public Furnishing(IDataRecord record)
			{
				this.Id = (int)record["itemId"];
				this.Name = (string)record["name"];
			}
			#endregion

			#region Public Properties
			public int Id { get; set; }

			public string Name { get; set; }
			#endregion

			#region Public Static Methods
			public static Furnishing Create(IDataRecord record) => new(record);
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion
		}
		#endregion
	}
}

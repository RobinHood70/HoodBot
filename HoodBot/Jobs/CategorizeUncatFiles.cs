namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	[method: JobInfo("Categorize Uncategorized Files", "Maintenance")]
	public class CategorizeUncatFiles(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Static Fields
		private static readonly char[] Dash = ['-'];
		#endregion

		#region Fields
		private readonly UespNamespaceList nsList = new UespNamespaceList(jobManager.Site);
		#endregion

		#region Public Override Properties
		public override string LogName => "Categorize Uncategorized Files";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Add category";

		protected override void LoadPages() => this.Pages.GetQueryPage("Uncategorizedimages");

		protected override void ParseText(ContextualParser parser)
		{
			ArgumentNullException.ThrowIfNull(parser);
			var headerFound = false;
			foreach (var header in parser.HeaderNodes)
			{
				var title = header.Title.ToValue().Trim(TextArrays.EqualsSign).Trim();
				if (string.Equals(title, "Licensing", StringComparison.Ordinal))
				{
					headerFound = true;
				}
				else if (title.EndsWith(':'))
				{
					headerFound = true;
					header.Title.Clear();
					header.Title.AddText("== Licensing ==");
				}
			}

			if (this.CategoryFromName(parser.Page.Title.PageName) is string cat)
			{
				if (!headerFound)
				{
					var nodes = parser;
					var factory = parser.Factory;
					if (nodes.Count > 0)
					{
						nodes.AddText("\n\n");
					}

					nodes.Add(factory.HeaderNodeFromParts(2, " Licensing "));
					nodes.AddText("\n");
					nodes.Add(factory.TemplateNodeFromParts("Sfwimage"));
				}

				// Debug.WriteLine($"*{cat}: [[:{parsedPage.Context.FullPageName}|]]");
				parser.AddCategory(cat, false);
			}
		}
		#endregion

		#region Private Methods
		private string? CategoryFromName(string pageName)
		{
			var split = pageName.Split(Dash, 4);
			if (split.Length >= 3 &&
				this.nsList.TryGetValue(split[0], out var uespNamespace))
			{
				var next = 1;
				var itemCat = split[next];
				if (split.Length == 4 && string.Equals(itemCat, "icon", StringComparison.OrdinalIgnoreCase))
				{
					next++;
					itemCat += '-' + split[next];
				}

				itemCat = itemCat.ToLowerInvariant();
				var cat = itemCat switch
				{
					/*
					"armor" or "icon-armor" => "Icons-Armor",
					"clothing" or "icon-clothing" => "Icons-Clothing",
					"ing" or "icon-ingredient" => "Icons-Ingredients",
					"map" => "Map Images",
					"mmw" => "Morrowind Modding Wiki Images",
					"npc" => "NPC Images",
					"quest" => "Quest Images",
					"weapon" or "icon-weapon" => "Icons-Weapons",
					"place" => "Place Images",
					"interior" => "Interior Images",
					*/
					"item" => "Item Images",
					_ => null,
				};

				if (cat != null)
				{
					return uespNamespace.Category + '-' + cat;
				}
			}

			return null;
		}
		#endregion
	}
}
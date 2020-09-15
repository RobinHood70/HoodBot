namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.Parser;

	public class BladesPoisons : EditJob
	{
		#region Static Fields
		private static readonly Regex DescriptionFinder = new Regex(@"^:(?<name>.*?) - (?<desc>.*?)$", RegexOptions.Multiline);
		private static readonly Regex RowFinder = new Regex(@"\|-\n!rowspan=2\|\d+\n\|rowspan=2\|(?<baseRecipe>.*?)\n\|colspan=2\|(?<names>.*?)\n\|-\n\|(?<matEffects>.*?)\n");
		private static readonly string[] Effects = new[]
		{
			"Blades:Weakness to Fire",
			"Blades:Weakness to Frost",
			"Blades:Weakness to Shock",
			"Blades:Weakness to Poison",
			"Blades:Damage Health Regeneration",
			"Blades:Damage Stamina Regeneration",
			"Blades:Damage Magicka Regeneration",
		};
		#endregion

		#region Fields
		private readonly Dictionary<string, string> descriptions = new Dictionary<string, string>(StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("Create/Update Blades Potions"/*, "Blades"*/)]
		public BladesPoisons(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var pages = PageCollection.Unlimited(this.Site);
			pages.GetTitles("User:RobinHood70/He", "Blades:Alchemy");
			this.ParseDescriptions(pages["User:RobinHood70/He"].Text);
			this.Pages.GetCategoryMembers("Blades-Items-Potions");
			this.CreatePages(pages["Blades:Alchemy"].Text);
		}

		protected override void Main() => this.SavePages("Create/update potion info", false);
		#endregion

		#region Private Static Methods
		private void CreatePages(string text)
		{
			var matches = (IList<Match>)RowFinder.Matches(text);
			foreach (var row in matches)
			{
				var baseRecipe = row.Groups["baseRecipe"].Value.Trim();
				var nameNodes = new List<LinkNode>(NodeCollection.Parse(row.Groups["names"].Value).FindAll<LinkNode>());
				var matEffects = row.Groups["matEffects"].Value.Split("||");
				if (nameNodes.Count != 7 || matEffects.Length != 14)
				{
					throw new InvalidOperationException();
				}

				for (var i = 0; i < 7; i++)
				{
					var name = WikiTextVisitor.Value(nameNodes[i].Title);
					var title = Title.FromName(this.Site, name);
					var recipe = matEffects[i * 2] + "<br>" + baseRecipe;
					var effect = matEffects[i * 2 + 1];
					/*
					if (this.Pages.TryGetValue(title.FullPageName, out var potionPage))
					{
						var nodes = NodeCollection.Parse(potionPage.Text);
						if (nodes.FindFirst<TemplateNode>(node => node.GetTitleValue() == "Blades Item Summary") is TemplateNode template)
						{
							template.AddParameter("recipe", recipe);
							potionPage.Text = WikiTextVisitor.Raw(nodes);
						}
						else
						{
							throw new InvalidOperationException();
						}
					}
					else
					{ */
					if (this.descriptions.TryGetValue(title.PageName, out var desc) && this.Pages.TryGetValue(title.FullPageName, out var page))
					{
						page.Text = $"{{{{Pre-Release}}}}{{{{Minimal}}}}{{{{Blades Item Summary\n|q=c\n|type=Poison\n|buy={{{{huh}}}}\n|sell={{{{huh}}}}\n|description={desc}\n|effect=* [[{Effects[i]}|{effect}]]\n|recipe={recipe}\n}}}}\n'''{title.PageName}''' {{{{huh}}}}\n\n{{{{Stub|Item}}}}";

						this.Pages.Add(page);
					}
				}
			}
		}

		private void ParseDescriptions(string text)
		{
			foreach (var match in (IEnumerable<Match>)DescriptionFinder.Matches(text))
			{
				this.descriptions.Add(match.Groups["name"].Value.Trim(), match.Groups["desc"].Value.Trim());
			}
		}
		#endregion
	}
}

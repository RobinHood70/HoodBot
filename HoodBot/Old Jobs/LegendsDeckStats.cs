namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Update Deck Magicka Stats", "Legends")]
	public class LegendsDeckStats(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Fields
		private readonly Dictionary<string, int> cardPowers = new(StringComparer.Ordinal);
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages() => this.GetCardPowers();

		protected override string GetEditSummary(Page page) => "Add magicka stats";

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Legends Deck Summary", BacklinksTypes.EmbeddedIn);

		protected override void ParseText(SiteParser parser)
		{
			ArgumentNullException.ThrowIfNull(parser);
			SortedDictionary<int, int> powerCount = [];
			if (parser.FindTemplate("Legends Deck Summary") is not ITemplateNode deckSummary)
			{
				throw new InvalidOperationException();
			}

			foreach (var template in parser.FindTemplates("Decklist"))
			{
				// The following lines set up the structure to handle skipNotes and skipQuantity, even though these are not currently used on any affected pages.
				List<IParameterNode> specialParams = new(template.FindAll("skipQuantity", "skipNotes"));
				var paramCount = 3 - specialParams.Count;
				foreach (var cluster in template.ParameterCluster(paramCount))
				{
					var cardName = cluster[0]?.Value.ToValue() ?? throw new InvalidOperationException();
					var quantity = paramCount < 2
						? 1
						: int.Parse(cluster[1]?.Value.ToValue() ?? throw new InvalidOperationException(), CultureInfo.InvariantCulture);
					var cardPower = this.cardPowers[cardName];
					if (!powerCount.ContainsKey(cardPower))
					{
						powerCount.Add(cardPower, 0);
					}

					powerCount[cardPower] += quantity;
				}
			}

			SiteNodeFactory factory = new(this.Site);
			foreach (var entry in powerCount)
			{
				var paramName = "m" + entry.Key.ToString(CultureInfo.InvariantCulture);
				var paramValue = entry.Value.ToString(CultureInfo.InvariantCulture) + '\n';
				if (deckSummary.Find(paramName) is IParameterNode param)
				{
					param.Value.Clear();
					param.Value.AddText(paramValue);
				}
				else
				{
					param = factory.ParameterNodeFromParts(paramName, paramValue);
					deckSummary.Parameters.Add(param);
				}
			}
		}
		#endregion

		#region Private Methods
		private void GetCardPowers()
		{
			const string mtCostName = "cost";
			var cards = this.Site.CreateMetaPageCollection(PageModules.Custom, true, mtCostName);
			cards.SetLimitations(LimitationType.OnlyAllow, UespNamespaces.Legends);
			cards.GetCategoryMembers("Legends-Cards", CategoryMemberTypes.Page, false);

			foreach (var page in cards)
			{
				if (page is VariablesPage varPage && varPage.MainSet != null)
				{
					this.cardPowers.Add(page.Title.PageName, int.TryParse(varPage.GetVariable(mtCostName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var power) ? power : 0);
				}
			}
		}
		#endregion
	}
}
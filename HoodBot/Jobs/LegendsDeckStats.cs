namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class LegendsDeckStats : ParsedPageJob
	{
		#region Fields
		private readonly Dictionary<string, int> cardPowers = new(StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("Update Deck Magicka Stats", "Legends")]
		public LegendsDeckStats(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Add magicka stats";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.GetCardPowers();
			base.BeforeLogging();
		}

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Legends Deck Summary", BacklinksTypes.EmbeddedIn);

		protected override void ParseText(object sender, ContextualParser parser)
		{
			SortedDictionary<int, int> powerCount = new();
			if (parser.NotNull(nameof(parser)).FindSiteTemplate("Legends Deck Summary") is not SiteTemplateNode deckSummary)
			{
				throw new InvalidOperationException();
			}

			foreach (var template in parser.FindSiteTemplates("Decklist"))
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
			cards.SetLimitations(LimitationType.FilterTo, UespNamespaces.Legends);
			cards.GetCategoryMembers("Legends-Cards", CategoryMemberTypes.Page, false);

			foreach (var page in cards)
			{
				if (page is VariablesPage varPage && varPage.MainSet != null)
				{
					this.cardPowers.Add(page.PageName, int.TryParse(varPage.GetVariable(mtCostName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var power) ? power : 0);
				}
			}
		}
		#endregion
	}
}
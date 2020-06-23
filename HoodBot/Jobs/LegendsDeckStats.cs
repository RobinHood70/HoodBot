namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class LegendsDeckStats : ParsedPageJob
	{
		#region Fields
		private readonly Dictionary<string, int> cardPowers = new Dictionary<string, int>();
		#endregion

		#region Constructors
		[JobInfo("Update Deck Magicka Stats", "Legends")]
		public LegendsDeckStats([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
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

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			var powerCount = new SortedDictionary<int, int>();
			var deckSummary = parsedPage.FindFirst<TemplateNode>(node => node.GetTitleValue() == "Legends Deck Summary");
			if (deckSummary == null)
			{
				throw new InvalidOperationException();
			}

			foreach (var template in parsedPage.FindAll<TemplateNode>(node => node.GetTitleValue() == "Decklist"))
			{
				// The following lines set up the structure to handle skipNotes and skipQuantity, even though these are not currently used on any affected pages.
				var specialParams = new List<ParameterNode>(template.FindParameters("skipQuantity", "skipNotes"));
				var paramCount = 3 - specialParams.Count;
				foreach (var cluster in template.ParameterCluster(paramCount))
				{
					var cardName = cluster[0]?.ValueToText() ?? throw new InvalidOperationException();
					var quantity = paramCount < 2
						? 1
						: int.Parse(cluster[1]?.ValueToText() ?? throw new InvalidOperationException(), CultureInfo.InvariantCulture);
					var cardPower = this.cardPowers[cardName];
					if (!powerCount.ContainsKey(cardPower))
					{
						powerCount.Add(cardPower, 0);
					}

					powerCount[cardPower] += quantity;
				}
			}

			foreach (var entry in powerCount)
			{
				var paramName = "m" + entry.Key.ToString();
				var paramValue = entry.Value.ToString() + '\n';
				if (deckSummary.FindParameter(paramName) is ParameterNode param)
				{
					param.Value.Clear();
					param.Value.AddText(paramValue);
				}
				else
				{
					param = ParameterNode.FromParts(paramName, paramValue);
					deckSummary.Parameters.Add(param);
				}
			}
		}
		#endregion

		#region Private Methods
		private void GetCardPowers()
		{
			const string mtCostName = "cost";
			var pageLoadOptions = new PageLoadOptions(PageModules.Custom, true);
			var pageCreator = new MetaTemplateCreator(mtCostName);
			var cards = new PageCollection(this.Site, pageLoadOptions, pageCreator);
			cards.SetLimitations(LimitationType.FilterTo, UespNamespaces.Legends);
			cards.GetCategoryMembers("Legends-Cards", CategoryMemberTypes.Page, false);

			foreach (var page in cards)
			{
				if (page is VariablesPage varPage && varPage.MainSet != null)
				{
					this.cardPowers.Add(page.PageName, int.TryParse(varPage.GetVariable(mtCostName), out var power) ? power : 0);
				}
			}
		}
		#endregion
	}
}
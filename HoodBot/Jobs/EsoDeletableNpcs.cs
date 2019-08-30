﻿namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	internal class EsoDeletableNpcs : WikiJob
	{
		#region Constructors
		[JobInfo("Find deletable NPCs", "ESO")]
		public EsoDeletableNpcs(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.Site.UserFunctions.SetResultTitle(ResultDestination.ResultsPage, "ESO NPC pages with no matching database entry");

			this.StatusWriteLine("Getting NPC data from database");
			var unfilteredNpcList = EsoGeneral.GetNpcsFromDatabase(this.Site.Namespaces[UespNamespaces.Online]);
			var allNames = new List<string>();
			foreach (var npc in unfilteredNpcList)
			{
				allNames.Add(npc.Name);
			}

			allNames.Sort();

			this.StatusWriteLine("Getting NPC data from wiki");
			var allNpcs = new TitleCollection(this.Site);
			var templates = new TitleCollection(this.Site.Namespaces[MediaWikiNamespaces.Template], "Online NPC Summary");
			allNpcs.GetPageTranscludedIn(templates);
			//// allNpcs.GetCategoryMembers("Online-NPCs", CategoryMemberTypes.Page, false);
			//// allNpcs.GetCategoryMembers("Online-Creatures-All", CategoryMemberTypes.Page, false);
			allNpcs.Sort();

			Debug.WriteLine("== ESO NPCs with No Corresponding Entries in the ESO Database ==");
			foreach (var page in allNpcs)
			{
				var npc = allNames.BinarySearch(page.PageName);
				if (npc < 0)
				{
					npc = allNames.BinarySearch(page.LabelName);
					if (npc < 0)
					{
						Debug.WriteLine($"* [[{page.FullPageName}|{page.LabelName}]]");
					}
				}
			}
		}
		#endregion
	}
}
namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	internal sealed class EsoDeletableNpcs : WikiJob
	{
		#region Constructors
		[JobInfo("Find Deletable NPCs", "ESO")]
		public EsoDeletableNpcs(JobManager jobManager)
			: base(jobManager)
		{
			this.SetResultDescription("ESO NPC pages with no matching database entry");
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.StatusWriteLine("Getting NPC data from database");
			var unfilteredNpcList = EsoLog.GetNpcs();
			foreach (var dupe in unfilteredNpcList.Duplicates)
			{
				this.Warn($"Warning: an NPC with the name \"{dupe.DataName}\" exists more than once in the database!");
			}

			List<string> allNames = new();
			foreach (var npc in unfilteredNpcList)
			{
				allNames.Add(npc.DataName);
			}

			allNames.Sort(StringComparer.Ordinal);

			this.StatusWriteLine("Getting NPC data from wiki");
			TitleCollection allNpcs = new(this.Site);
			TitleCollection templates = new(this.Site, MediaWikiNamespaces.Template, "Online NPC Summary");
			allNpcs.GetPageTranscludedIn(templates);
			//// allNpcs.GetCategoryMembers("Online-NPCs", CategoryMemberTypes.Page, false);
			//// allNpcs.GetCategoryMembers("Online-Creatures-All", CategoryMemberTypes.Page, false);
			allNpcs.Sort();

			Debug.WriteLine("== ESO NPCs with No Corresponding Entries in the ESO Database ==");
			foreach (var title in allNpcs)
			{
				var npc = allNames.BinarySearch(title.PageName, StringComparer.Ordinal);
				if (npc < 0)
				{
					var labelName = title.LabelName();
					npc = allNames.BinarySearch(labelName, StringComparer.Ordinal);
					if (npc < 0)
					{
						Debug.WriteLine($"* [[{title.FullPageName}|{labelName}]]");
					}
				}
			}
		}

		protected override void JobCompleted()
		{
			this.Results?.Save();
			base.JobCompleted();
		}
		#endregion
	}
}
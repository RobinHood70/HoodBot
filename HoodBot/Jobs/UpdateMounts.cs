namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Update Mount IDs", "ESO")]
	public class UpdateMounts(JobManager jobManager) : EditJob(jobManager)
	{
		#region Fields
		private readonly SortedList<string, int> ids = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region Protected Override Methods
		protected override void AfterLoadPages()
		{
			this.WriteLine("Found in database but not in Online-Mounts.");
			foreach (var dbMount in this.ids)
			{
				this.WriteLine("* " + dbMount.Key);
			}
		}

		protected override string GetEditSummary(Page page) => "Update mount ID";

		protected override void LoadPages()
		{
			var query = "SELECT id, name FROM uesp_esolog.collectibles WHERE categoryType = 2 AND furnCategory = 'Mounts'";
			foreach (var row in Database.RunQuery(EsoLog.Connection, query))
			{
				this.ids.Add(EsoLog.ConvertEncoding((string)row["name"]), (int)(long)row["id"]);
			}

			this.Pages.GetCategoryMembers("Online-Mounts", CategoryMemberTypes.Page, false);
		}

		protected override void PageLoaded(Page page)
		{
			var idPageName = page.Title.PageName.Replace(" (mount)", string.Empty, StringComparison.Ordinal);
			if (this.ids.TryGetValue(idPageName, out var id))
			{
				this.ids.Remove(idPageName);
				ContextualParser parser = new(page);
				if (parser.FindSiteTemplate("Online Collectible Summary") is ITemplateNode template)
				{
					template.Update("id", id.ToStringInvariant());
					parser.UpdatePage();
				}
			}
		}
		#endregion
	}
}
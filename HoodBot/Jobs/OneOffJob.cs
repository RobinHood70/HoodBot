namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class OneOffJob : WikiJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods

		protected override void Main()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(UespNamespaces.MorrowindMod);
			titles.GetNamespace(UespNamespaces.OblivionMod);
			titles.GetNamespace(UespNamespaces.SkyrimMod);
			titles.GetNamespace(UespNamespaces.Mod);

			var remove = new TitleCollection(this.Site);
			remove.GetCategoryMembers("Morrowind Mod-Modding-Functions");
			remove.GetCategoryMembers("Morrowind Mod-Modding-Mod File Format");
			remove.GetCategoryMembers("Oblivion Mod-Modding-Mod File Format");
			remove.GetCategoryMembers("Skyrim Mod-File Formats-Mod File Format");
			remove.GetCategoryMembers("Skyrim Mod-File Formats-Mod File Format-Fields");

			foreach (var title in remove)
			{
				titles.Remove(title);
			}

			var nsList = new UespNamespaceList(this.Site);
			foreach (var ns in nsList)
			{
				if (ns.IsPseudoNamespace)
				{
					for (var i = titles.Count - 1; i >= 0; i--)
					{
						var title = titles[i];
						if (title.Namespace == ns.BaseTitle.Namespace
							&& title.PageName.StartsWith(ns.BaseTitle.PageName, StringComparison.Ordinal))
						{
							titles.RemoveAt(i);
						}
					}
				}
			}

			titles.Sort();
			foreach (var title in titles)
			{
				Debug.WriteLine(title.AsLink(false));
			}
		}
		#endregion
	}
}
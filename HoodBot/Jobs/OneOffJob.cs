namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.LogDetails = "Date retcon";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.Pages.GetNamespace(UespNamespaces.Lore, Filter.Exclude);
			this.Pages.Remove("Lore:Birds of Wrothgar (old)");
			foreach (var page in this.Pages)
			{
				page.Text = page.Text.Replace("2E 583", "2E 582", StringComparison.OrdinalIgnoreCase);
			}
		}

		protected override void Main() => this.SavePages("Date retcon");
		#endregion
	}
}
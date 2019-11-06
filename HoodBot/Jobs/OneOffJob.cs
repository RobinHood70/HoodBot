namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class OneOffJob : WikiJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Public Override Properties
		//// public override string LogName => "Check for Jeancey";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var results = PageCollection.Unlimited(this.Site);
			results.GetSearchResults("Riddle'thar", this.Site.Namespaces.RegularIds);
			foreach (var page in results)
			{
				if (page.Text.IndexOf("Riddle'thar", StringComparison.CurrentCulture) >= 0)
				{
					this.StatusWriteLine(page.FullPageName);
				}
			}
		}
		#endregion
	}
}

namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			Regex wordCount = new(@"[\w-]+", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.Pages.GetCategoryMembers("Lore-Books", CategoryMemberTypes.Page, true);
			this.Pages.Sort();
			foreach (var page in this.Pages)
			{
				ContextualParser parsedText = new(page);
				if (parsedText.Nodes.ToValue() is string reText &&
					wordCount.Matches(reText) is MatchCollection matches)
				{
					if (matches.Count < 25)
					{
						Debug.WriteLine("Words:");
						foreach (var match in matches)
						{
							Debug.WriteLine("  " + match);
						}
					}

					Debug.WriteLine($"{page.FullPageName}: {matches.Count} words");
				}
			}
		}

		protected override void Main()
		{
		}
		#endregion
	}
}
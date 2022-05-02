namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

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
			var hide = new Regex(@"{{Hide\|(?<value>.+)}}", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.Pages.GetBacklinks("Template:Hide");
			foreach (var page in this.Pages)
			{
				page.Text = page.Text.Replace("!class=sort_desc|", "!", StringComparison.Ordinal);
				if (!page.Text.Contains("{{Key Table", StringComparison.Ordinal))
				{
					page.Text = hide.Replace(page.Text, ReplaceHide);
				}
			}
		}

		protected override void Main() => this.SavePages("Update old sorting");
		#endregion

		#region Private Methods
		private static string ReplaceHide(Match match)
		{
			var value = match.Groups["value"].Value.Trim();
			return match.Success &&
				string.Equals(value, "ZZ", StringComparison.Ordinal)
					? "{{Blank}}"
					: "{{Sort|" + value + "}}|";
		}
		#endregion
	}
}
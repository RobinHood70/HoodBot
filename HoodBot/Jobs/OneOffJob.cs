namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
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
			var rowFinder = new Regex(@"\|-\ *\n[\|\t](\{\{[Ii]con\|(?<icontype>[^|]*)\|(?<icon>[^\}]*)\}\})?\ *\t\ *(\{\{(LIL|Linkable Item Link)\|(?<item>[^|]*)\|questid=(?<id>\d*)\}\}|\[\[(ON|Online):(?<item>.*?)\|.*?\]\])\ *\t\ *(?<loc>.*?)\ *\t\ *\[\[(ON|Online):(?<quest>.*?)\|.*?\]\]\ *\t\ *(?<desc>.*)", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.Pages.GetCategoryMembers("Online-Items-Quest Items", CategoryMemberTypes.Page, true);
			foreach (var page in this.Pages)
			{
				page.Text = page.Text
					.Replace("|||", "||", StringComparison.Ordinal)
					.Replace("||", "\t", StringComparison.Ordinal);
				page.Text = rowFinder.Replace(page.Text, "{{Online Quest Item Entry|${item}|icontype=${icontype}|icon=${icon}|id=${id}|loc=${loc}|quest=${quest}|${desc}}}");
				page.Text = page.Text
					.Replace("\t", "||", StringComparison.Ordinal)
					.Replace("</noinclude>\n<noinclude>", "\n", StringComparison.OrdinalIgnoreCase)
					.Replace("</noinclude>\n<includeonly>", "</noinclude><includeonly>", StringComparison.OrdinalIgnoreCase)
					.Replace("\n<noinclude>{{Online Quest Items", "<noinclude>\n{{Online Quest Items", StringComparison.OrdinalIgnoreCase);
			}
		}

		protected override void Main() => this.SavePages("Bot-assisted: Convert to template", false);
		#endregion
	}
}
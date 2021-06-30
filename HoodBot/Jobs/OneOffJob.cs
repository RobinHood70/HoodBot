namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	public class OneOffJob : EditJob
	{
		#region Static Fields
		private static readonly Regex RowFinder = new(@"\|-\ *\n[!\|]\ *\[\[(?<icon>.*?)(?<iconSize>\|.*?)?\]\]\ *\n[!\|]\ *\{\{Anchor\|(?<itemName>.*?)\}\}\ *(\|\||<br>)\ *(\{\{Small\|)?(?<id>.*)(\}\})?\n\|\ *(?<weight>.*?)\ *\|\|\ *(?<value>.*?)\ *\n\|(\{\{AL\|L\}\}\|)?(?<desc>.*?)\n(\|\ *\[\[(?<image>.*?)(?<imageSize>\|.*?)?\]\]\ *\n)?", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		#endregion

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
			var pageText = this.Site.LoadPageText("Morrowind:Miscellaneous Items");
			if (pageText != null)
			{
				pageText = pageText
					.Replace("{{sic|Dewmer|Dwemer|nolink=1}}", "Dewmer", StringComparison.OrdinalIgnoreCase)
					.Replace("{{Anchor|The Head Of Scourge|The Head of Scourge}}", "{{Anchor|The Head Of Scourge}}", StringComparison.OrdinalIgnoreCase);
				var matches = (IReadOnlyCollection<Match>)RowFinder.Matches(pageText);
				Debug.WriteLine(matches.Count);
				foreach (var match in matches)
				{
					var groups = match.Groups;
					var pageName = groups["itemName"].Value;
					Debug.WriteLine("Match: " + match.Value);
					var title = new Title(this.Site.Namespaces[UespNamespaces.Morrowind], pageName);
					if (!this.Pages.TryGetValue(title, out var page))
					{
						page = new Page(title);
						this.Pages.Add(page);
					}
					else
					{
						page.Text += "\n{{NewLine}}\n";
					}

					page.Text += "{{Item Summary" +
						"\n|objectid=" + groups["id"].Value +
						"\n|icon=" + Title.FromName(this.Site, groups["icon"].Value).PageName +
						"\n|image=" + Title.FromName(this.Site, groups["image"].Value).PageName +
						"\n|weight=" + groups["weight"].Value +
						"\n|value=" + groups["value"].Value +
						"\n}}" +
						"\n" + groups["desc"].Value;
				}
			}

			foreach (var page in this.Pages)
			{
				page.Text = "{{Trail|Items}}{{Minimal}}\n" + page.Text;
			}
		}

		protected override void Main() => this.SavePages("Create item page", false);
		#endregion
	}
}
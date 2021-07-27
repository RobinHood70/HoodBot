namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;
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
				var sb = new StringBuilder();
				foreach (var match in matches)
				{
					var groups = match.Groups;
					var pageName = groups["itemName"].Value;
					Debug.WriteLine("Match: " + match.Value);
					var title = new Title(this.Site.Namespaces[UespNamespaces.Morrowind], pageName);
					sb.Clear();
					if (!this.Pages.TryGetValue(title, out var page))
					{
						page = new Page(title);
						this.Pages.Add(page);
					}
					else
					{
						sb
							.Append(page.Text)
							.Append("\n{{NewLine}}\n");
					}

					sb
						.AppendLinefeed("{{Item Summary")
						.Append("|objectid=")
						.AppendLinefeed(groups["id"].Value)
						.Append("|icon=")
						.AppendLinefeed(Title.FromName(this.Site, groups["icon"].Value).PageName)
						.Append("|image=")
						.AppendLinefeed(Title.FromName(this.Site, groups["image"].Value).PageName)
						.Append("|weight=")
						.AppendLinefeed(groups["weight"].Value)
						.Append("|value=")
						.AppendLinefeed(groups["value"].Value)
						.AppendLinefeed("}}")
						.Append(groups["desc"].Value);

					page.Text = sb.ToString();
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
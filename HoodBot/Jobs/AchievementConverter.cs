namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	public class AchievementConverter : EditJob
	{
		#region Private Constants
		private const string TemplateName = "ESO Achievements List";
		#endregion

		#region Static Fields
		private static readonly Regex AchTableFinder = new(@"\{\|\ *class=""?wikitable""?\ *\n!(\ *colspan=(?<colspan>[23])\ *\||\ +(!!|\|\||\n[!|\|]))\ *Achievement\ *(!!|\|\||\n[!|\|])\ *Points\ *(!!|\|\||\n[!|\|])\ *Description(?<hasreward>\ *(!!|\|\||\n[!|\|])\ *Reward)?\ *\n((\|-\ *)?\n)*(?<content>(?<template>\{\{Online Achievement Entry\|[^}]*\}\})(?<reward>.*?)?\n)+((\|-\ *)?\n)*\|\}", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Fields
		private readonly HashSet<string> titles = new(StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("Achievement Converter")]
		public AchievementConverter(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.Pages.PageLoaded += this.ParsePage;
			this.Pages.GetBacklinks("Template:Online Achievement Entry");
			//// this.Pages.GetTitles("Online:Fighters Guild");
			this.Pages.PageLoaded -= this.ParsePage;

			var purgeTitles = new TitleCollection(this.Site, UespNamespaces.Online, this.titles);
			var purgePages = this.Site.CreateMetaPageCollection(PageModules.Info, false, "iconname");
			purgePages.GetTitles(purgeTitles);
			purgeTitles.Clear();
			foreach (var page in purgePages)
			{
				if (page.Exists &&
					page is VariablesPage metaPage &&
					metaPage.MainSet is IReadOnlyDictionary<string, string> mainSet &&
					!mainSet.ContainsKey("iconname"))
				{
					purgeTitles.Add(page);
				}
			}

			if (purgeTitles.Count > 0)
			{
				this.StatusWriteLine("Purging pages");
				this.ProgressMaximum = purgeTitles.Count;
				this.Progress = 0;
				var purgeGroup = new TitleCollection(this.Site);
				foreach (var page in purgeTitles)
				{
					purgeGroup.Add(page);
					if (purgeGroup.Count == 10)
					{
						purgeGroup.Purge(PurgeMethod.LinkUpdate);
						purgeGroup.Clear();
					}

					this.Progress++;
				}

				purgeGroup.Purge(PurgeMethod.LinkUpdate);
			}
		}

		protected override void Main() => this.SavePages("Convert to " + TemplateName, true, this.ParsePage);
		#endregion

		#region Private Static Methods
		private void ParsePage(object sender, Page page)
		{
			var newPage = new StringBuilder();
			var oldTextIndex = 0;
			var factory = new WikiNodeFactory();
			var currentReward = false;
			if (AchTableFinder.Matches(page.Text) is IEnumerable<Match> matches)
			{
				var savedReward = currentReward;
				foreach (var match in matches)
				{
					newPage.Append(page.Text[oldTextIndex..match.Index]);
					oldTextIndex = match.Index + match.Length;
					if (this.ParseRow(match, factory, ref currentReward) is StringBuilder newText)
					{
						newPage.Append(newText);
					}
					else
					{
						newPage.Append(match.Value);
						currentReward = savedReward;
					}
				}
			}

			newPage.Append(page.Text[oldTextIndex..]);
			page.Text = newPage.ToString();
		}

		private StringBuilder? ParseRow(Match match, WikiNodeFactory factory, ref bool currentReward)
		{
			var sb = new StringBuilder();
			var hasReward = match.Groups["hasreward"].Success;
			if (hasReward != currentReward)
			{
				currentReward = hasReward;
				sb
					.Append("{{#local:showreward|")
					.Append(hasReward ? "1" : string.Empty)
					.Append("}}");
			}

			sb
				.Append("{{")
				.Append(TemplateName);
			var captureCount = match.Groups["content"].Captures.Count;
			var newLine = captureCount > 1 ? "\n" : string.Empty;
			for (var i = 0; i < captureCount; i++)
			{
				var template = factory.SingleNode<ITemplateNode>(match.Groups["template"].Captures[i].Value);
				template.Remove("noline");
				if (i == 0)
				{
					template.Remove("ThickLine");
				}

				if (string.Equals(match.Groups["colspan"].Value, "3", StringComparison.Ordinal))
				{
					template.Remove("colspan");
				}

				if (template.Parameters.Count > 1)
				{
					return null;
				}

				if (template.Find(1) is IParameterNode nameParameter)
				{
					var name = nameParameter.Value.ToValue();
					this.titles.Add(name);
					sb
						.Append(newLine)
						.Append('|')
						.Append(name);
					if (hasReward)
					{
						sb
							.Append('|')
							.Append(match.Groups["reward"].Captures[i].Value.TrimStart('|', ' ').TrimEnd());
					}
				}
				else
				{
					Debug.WriteLine("Malformed template:\n" + match.Value);
					return null;
				}
			}

			sb
				.Append(newLine)
				.Append("}}");
			return sb;
		}
		#endregion
	}
}

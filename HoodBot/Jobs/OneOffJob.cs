namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class OneOffJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Fix header to match link text";

		protected override void LoadPages()
		{
			var titles = new TitleCollection(this.Site);
			var titleConverter = new ISimpleTitleJsonConverter(this.Site);
			var repFile = File.ReadAllText(Environment.ExpandEnvironmentVariables(@"%BotData%\Replacements - Merge.json"));
			var reps = JsonConvert.DeserializeObject<IEnumerable<Replacement>>(repFile, titleConverter) ?? throw new InvalidOperationException();
			foreach (var rep in reps)
			{
				titles.Add(rep.To);
			}

			this.Pages.GetTitles(titles);
		}
		#endregion

		#region Protected Override Methods
		protected override void ParseText(object sender, Page page, ContextualParser parsedPage)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(parsedPage, nameof(parsedPage));
			var dbHeader = parsedPage.FindFirst<HeaderNode>(header => header.Level == 1 && header.GetInnerText(true) == "Dragonborn {{DB}}");
			if (dbHeader != null)
			{
				dbHeader.Title.Clear();
				dbHeader.Title.AddText("= Dragonborn =");
			}
		}
		#endregion
	}
}
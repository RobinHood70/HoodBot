namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static Properties.Resources;
	using static RobinHood70.WikiCommon.Globals;

	public class HoodBotFunctions : UserFunctions
	{
		#region Static Fields
		private static readonly Regex CurrentTaskFinder = SectionFinder("Current Task");
		private static readonly Regex EntryFinder = Template.Find(null, "/Entry", "\n");
		private static readonly Regex EntryTableFinder = new Regex(@"(?<=id=""EntryTable"".*?)\|\}", RegexOptions.Singleline);
		private static readonly Regex TaskLogFinder = SectionFinder("Task Log");
		#endregion

		#region Fields
		private readonly Dictionary<ResultDestination, ResultInfo> results = new Dictionary<ResultDestination, ResultInfo>();
		private readonly Dictionary<ResultDestination, StringBuilder> stringBuilders = new Dictionary<ResultDestination, StringBuilder>();
		private LogInfo lastLogInfo;
		private TitleCollection talkLikePages = null;
		#endregion

		#region Constructors
		public HoodBotFunctions(Site site)
			: base(site)
		{
			this.NativeAbstractionLayer = site.AbstractionLayer as WikiAbstractionLayer;
			this.NativeClient = this.NativeAbstractionLayer.Client;
			var pageName = site.User.FullPageName;
			this.LogPage = new Page(site, pageName + "/Log");
			this.RequestsPage = new Page(site, MediaWikiNamespaces.Project, "Bot Requests");
			this.PrivateResetResultsPage();
			this.StatusPage = this.LogPage;
			this.DefaultResultDestination = ResultDestination.ResultsPage;
		}
		#endregion

		#region Public Override Properties
		public override IReadOnlyList<string> DeleteTemplates { get; } = new[] { "Proposeddeletion", "Prod", "Speed", "Speedydeletion" };

		public override IReadOnlyList<string> DoNotDeleteTemplates { get; } = new[] { "Empty category", "Linked image" };

		public override LogJobTypes LogJobTypes => LogJobTypes.Write;

		public override TitleCollection TalkLikePages
		{
			get
			{
				if (this.talkLikePages == null)
				{
					var titles = new TitleCollection(this.Site);
					titles.GetCategoryMembers("Message Boards", false);
					this.talkLikePages = titles;
				}

				return this.talkLikePages;
			}
		}
		#endregion

		#region Internal Properties
		internal WikiAbstractionLayer NativeAbstractionLayer { get; }

		internal IMediaWikiClient NativeClient { get; }

		internal Page RequestsPage { get; }
		#endregion

		#region Public Static Methods
		public static UserFunctions CreateInstance(Site site) => new HoodBotFunctions(site);
		#endregion

		#region Public Override Methods
		public override ChangeStatus AddLogEntry(LogInfo info)
		{
			ThrowNull(info, nameof(info));
			var result = ChangeStatus.NoEffect;
			if (this.ShouldLog(info))
			{
				this.lastLogInfo = info;
				this.LogPage.PageLoaded += this.LogPage_AddEntry;
				result = ChangeStatus.Unknown; // Change to Unknown so we know if we've ever successfully saved.
				do
				{
					this.LogPage.Load();
					try
					{
						result = this.LogPage.Save("Job Started", false);
					}
					catch (EditConflictException)
					{
					}
					catch (StopException)
					{
						result = ChangeStatus.Cancelled;
					}
				}
				while (result == ChangeStatus.Unknown);

				this.LogPage.PageLoaded -= this.LogPage_AddEntry;
			}
			else
			{
				this.lastLogInfo = null;
			}

			return result;
		}

		public override void AddResult(ResultDestination destination, string text)
		{
			if (!this.stringBuilders.TryGetValue(destination, out var sb))
			{
				throw new InvalidOperationException($"Results for {destination} have not been initialized.");
			}

			sb.Append(text);
		}

		public override void DoSiteCustomizations()
		{
			var moduleFactory = this.NativeAbstractionLayer.ModuleFactory;
			moduleFactory.RegisterProperty<VariablesInput>(PropVariables.CreateInstance);
			moduleFactory.RegisterGenerator<VariablesInput>(PropVariables.CreateInstance);
		}

		public override ChangeStatus EndLogEntry()
		{
			var result = ChangeStatus.NoEffect;
			if (this.ShouldLog(this.lastLogInfo))
			{
				this.LogPage.PageLoaded += this.LogPage_EndEntry;
				do
				{
					// Assumes that its current LogPage.Text is still valid and tries to update, then save that directly. Loads only if it gets an edit conflict.
					this.LogPage_EndEntry(this.LogPage, EventArgs.Empty);
					try
					{
						result = this.LogPage.Save("Job Finished", true);
					}
					catch (EditConflictException)
					{
						this.LogPage.Load();
					}
					catch (StopException)
					{
						result = ChangeStatus.Cancelled;
					}
				}
				while (result == ChangeStatus.Unknown);

				this.LogPage.PageLoaded -= this.LogPage_EndEntry;
			}

			return result;
		}

		public override void InitializeResult(ResultDestination destination, string user, string subject)
		{
			this.SetResultInfo(destination, user, subject);
			this.stringBuilders[destination] = new StringBuilder();
		}

		public override void OnAllJobsComplete()
		{
			foreach (var sb in this.stringBuilders)
			{
				if (sb.Value.Length > 0)
				{
					if (!this.results.TryGetValue(sb.Key, out var info))
					{
						throw new InvalidOperationException($"Result destination {sb.Key} was not properly initialized.");
					}

					info.Title ??= "Job Results";
					var result = sb.Value.ToString().Trim();
					switch (sb.Key)
					{
						case ResultDestination.Email:
							this.EmailResultsToUser(info.User, info.Title, result);
							break;
						case ResultDestination.LocalFile:
							File.WriteAllText(info.Title, result);
							break;
						case ResultDestination.ResultsPage:
							this.PostResultsToResultsPage(info.Title, result);
							break;
						case ResultDestination.UserTalkPage:
							this.PostResultsToUserTalkPage(info.User, info.Title, result);
							break;
						case ResultDestination.RequestPage:
							this.PostResultsToRequestPage(info.Title, result);
							break;
					}
				}
			}
		}

		public override void OnAllJobsStarting(int jobCount)
		{
			this.Site.ClearMessage(true);
			this.InitializeResult(ResultDestination.ResultsPage, null, "Job Results");
		}

		public override void ResetResultsPage() => this.PrivateResetResultsPage();

		public override void SetResultInfo(ResultDestination destination, string user, string title) => this.results[destination] = new ResultInfo(user, title);

		public override void SetResultTitle(ResultDestination destination, string title)
		{
			if (this.results.TryGetValue(destination, out var resultInfo))
			{
				resultInfo.Title = title;
			}
			else
			{
				resultInfo = new ResultInfo(null, title);
			}

			this.results[destination] = resultInfo;
		}

		public override ChangeStatus UpdateCurrentStatus(string status)
		{
			// In theory, this could make use of a SectionedPage, but that seems a bit overkill for a simple log page.
			ThrowNull(status, nameof(status));
			var taskSection = CurrentTaskFinder.Match(this.StatusPage.Text);
			if (!taskSection.Success)
			{
				throw BadLogPageException();
			}

			var insertPos = taskSection.Index + taskSection.Length;
			taskSection = TaskLogFinder.Match(this.StatusPage.Text, insertPos);
			var previousTask = this.StatusPage.Text.Substring(insertPos, taskSection.Index - insertPos);
			var currentTask = status + "\n\n";
			this.StatusPage.Text = this.StatusPage.Text
				.Remove(insertPos, taskSection.Index - insertPos)
				.Insert(insertPos, currentTask);
			return previousTask == currentTask ? ChangeStatus.NoEffect : ChangeStatus.Success;
		}
		#endregion

		#region Private Static Methods
		private static Exception BadLogPageException() => new FormatException(BadLogPage);

		private static Regex SectionFinder(string sectionName) => new Regex(@"^==\s*" + Regex.Escape(sectionName) + @"\s*==\s*?\n+", RegexOptions.Multiline);

		private static string UniversalNow() => DateTime.UtcNow.ToString("u").TrimEnd('Z');
		#endregion

		#region Private Methods
		private void EmailResultsToUser(string userName, string subject, string text)
		{
			var user = new User(this.Site, userName);
			user.Email(subject, text, false);
		}

		private void LogPage_AddEntry(Page sender, EventArgs eventArgs)
		{
			var result = this.UpdateCurrentStatus(this.lastLogInfo.Title + '.');
			var entry = EntryFinder.Match(sender.Text);
			if (!entry.Success)
			{
				entry = EntryTableFinder.Match(sender.Text);
				if (!entry.Success)
				{
					throw new FormatException(BadLogPage);
				}
			}
			else
			{
				var testTemplate = Template.Parse(entry.Value);
				if (result == ChangeStatus.NoEffect &&
					Parameter.IsNullOrEmpty(testTemplate["3"]) &&
					testTemplate["1"]?.Value == this.lastLogInfo.Title &&
					((testTemplate["info"]?.Value ?? string.Empty) == (this.lastLogInfo.Details ?? string.Empty)))
				{
					// If the last job was the same as this one, and is unfinished, then assume we're resuming the job and don't update.
					return;
				}
			}

			var entryTemplate = new Template("/Entry");
			entryTemplate.AddAnonymous(this.lastLogInfo.Title);
			if (!string.IsNullOrEmpty(this.lastLogInfo.Details))
			{
				entryTemplate.Add("info", this.lastLogInfo.Details);
			}

			entryTemplate.AddAnonymous(UniversalNow());
			this.LogPage.Text = this.LogPage.Text.Insert(entry.Index, entryTemplate.ToString() + "\n");
		}

		private void LogPage_EndEntry(Page sender, EventArgs eventArgs)
		{
			this.UpdateCurrentStatus("None.");
			var entry = EntryFinder.Match(sender.Text);
			if (!entry.Success)
			{
				throw BadLogPageException();
			}

			var entryTemplate = Template.Parse(entry.Value);
			if (entryTemplate["2"] == null || entryTemplate["3"] != null)
			{
				throw BadLogPageException();
			}

			entryTemplate.AddAnonymous(UniversalNow());
			entryTemplate.Sort("1", "info", "2", "3", "notes");

			sender.Text = sender.Text
				.Remove(entry.Index, entry.Length)
				.Insert(entry.Index, entryTemplate.ToString() + "\n");
		}

		private void PostResultsToRequestPage(string title, string result)
		{
			this.RequestsPage.Load();
			var sectionedPage = new SectionedPage(this.RequestsPage.Text);
			var section = sectionedPage.FindLastSection(title);
			var lastLine = Regex.Match(section.Text, "^(?<colons>:*).", RegexOptions.Multiline | RegexOptions.RightToLeft);
			var colons = lastLine.Success ? lastLine.Groups["colons"].Value : string.Empty;
			colons += ':';
			if (colons.Length > 6)
			{
				colons = "{{od}} ";
			}

			section.Text = section.Text.TrimEnd() + '\n' + colons + result;
			if (!result.Contains("~~~"))
			{
				section.Text += " ~~~~";
			}

			section.Text += "\n\n";

			this.RequestsPage.Text = sectionedPage.Build();
			this.RequestsPage.Save("Update status", false);
		}

		private void PostResultsToResultsPage(string title, string result)
		{
			if (this.ResultsPage != null)
			{
				this.ResultsPage.Text = result;
				this.ResultsPage.Save(title, false);
			}
		}

		private void PostResultsToUserTalkPage(string userName, string sectionTitle, string text)
		{
			var user = new User(this.Site, userName);
			user.NewTalkPageMessage(sectionTitle, text, "New Message from " + this.Site.User.Name);
		}

		private void PrivateResetResultsPage() => this.ResultsPage = new Page(this.Site, this.Site.User.FullPageName + "/Results");
		#endregion

		#region Private Classes
		private class ResultInfo
		{
			public ResultInfo(string user, string title)
			{
				this.Title = title;
				this.User = user;
			}

			public string Title { get; set; }

			public string User { get; set; }
		}
		#endregion
	}
}
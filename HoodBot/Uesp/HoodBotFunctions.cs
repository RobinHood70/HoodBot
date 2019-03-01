namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiClasses;
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
		private LogInfo lastLogInfo;
		#endregion

		#region Constructors
		public HoodBotFunctions(Site site)
			: base(site)
		{
			this.LogPage = new Page(this.Site, this.Site.User.FullPageName + "/Log");
			this.StatusPage = this.LogPage;
		}
		#endregion

		#region Public Override Properties
		public override LogJobTypes LogJobTypes => LogJobTypes.Write;
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

		public override void DoSiteCustomizations()
		{
			var wal = this.Site.AbstractionLayer as WallE.Eve.WikiAbstractionLayer;
			wal.ModuleFactory.RegisterProperty<VariablesInput>(PropVariables.CreateInstance);
			wal.ModuleFactory.RegisterGenerator<VariablesInput>(PropVariables.CreateInstance);
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
			var currentTask = status + "\n\n";
			this.StatusPage.Text = this.StatusPage.Text
				.Remove(insertPos, taskSection.Index - insertPos)
				.Insert(insertPos, currentTask);
			return taskSection.Value == currentTask ? ChangeStatus.NoEffect : ChangeStatus.Success;
		}
		#endregion

		#region Private Static Methods
		private static Exception BadLogPageException() => new FormatException(BadLogPage);

		private static Regex SectionFinder(string sectionName) => new Regex(@"^==\s*" + Regex.Escape(sectionName) + @"\s*==\s*?\n+", RegexOptions.Multiline);

		private static string UniversalNow() => DateTime.UtcNow.ToString("u").TrimEnd('Z');
		#endregion

		#region Private Methods
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
				var testTemplate = new Template(entry.Value);
				if (result == ChangeStatus.NoEffect &&
					Parameter.IsNullOrEmpty(testTemplate["3"]) &&
					testTemplate["1"]?.Value == this.lastLogInfo.Title &&
					(testTemplate["info"]?.Value ?? string.Empty) == (this.lastLogInfo.Details ?? string.Empty))
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

			var entryTemplate = new Template(entry.Value);
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
		#endregion
	}
}
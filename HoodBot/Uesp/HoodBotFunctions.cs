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

		#region Constructors
		public HoodBotFunctions(Site site)
			: base(site)
		{
		}
		#endregion

		#region Public Static Methods
		public static UserFunctions CreateInstance(Site site) => new HoodBotFunctions(site);
		#endregion

		#region Public Override Methods
		public override void AddLogEntry(LogInfo info)
		{
			ThrowNull(info, nameof(info));
			var result = ChangeStatus.Failed;
			do
			{
				this.LogPage.Load();
				this.UpdateCurrentStatus(this.LogPage, info.Title + '.');
				var entry = EntryFinder.Match(this.LogPage.Text);
				if (!entry.Success)
				{
					entry = EntryTableFinder.Match(this.LogPage.Text);
					if (!entry.Success)
					{
						throw new FormatException(BadLogPage);
					}
				}
				else
				{
					var testTemplate = new Template(entry.Value);
					if (
						Parameter.IsNullOrEmpty(testTemplate["3"]) &&
						testTemplate["1"]?.Value == info.Title &&
						(testTemplate["info"]?.Value ?? string.Empty) == (info.Details ?? string.Empty))
					{
						// If the last job was the same as this one, and is unfinished, then assume we're resuming the job and don't update.
						return;
					}
				}

				var entryTemplate = new Template("/Entry");
				entryTemplate.AddAnonymous(info.Title);
				if (!string.IsNullOrEmpty(info.Details))
				{
					entryTemplate.Add("info", info.Details);
				}

				entryTemplate.AddAnonymous(UniversalNow());
				this.LogPage.Text = this.LogPage.Text.Insert(entry.Index, entryTemplate.ToString() + "\n");
				try
				{
					result = this.LogPage.Save("Job Started", false);
				}
				catch (EditConflictException)
				{
				}
			}
			while (result.HasFlag(ChangeStatus.Failed));
		}

		public override void DoSiteCustomizations()
		{
			var wal = this.Site.AbstractionLayer as WallE.Eve.WikiAbstractionLayer;
			wal.ModuleFactory.RegisterProperty<VariablesInput>(PropVariables.CreateInstance);
			wal.ModuleFactory.RegisterGenerator<VariablesInput>(PropVariables.CreateInstance);
		}

		public override void EndLogEntry()
		{
			// Assumes that its current LogPage.Text is still valid and tries to save that directly. Loads only if it gets an edit conflict.
			var result = ChangeStatus.Failed;
			do
			{
				this.UpdateCurrentStatus(this.LogPage, "None.");
				var entry = EntryFinder.Match(this.LogPage.Text);
				if (!entry.Success)
				{
					throw new FormatException(BadLogPage);
				}

				var entryTemplate = new Template(entry.Value);
				entryTemplate.AddAnonymous(UniversalNow());
				entryTemplate.Sort("1", "info", "2", "3", "notes");

				this.LogPage.Text = this.LogPage.Text.Remove(entry.Index, entry.Length).Insert(entry.Index, entryTemplate.ToString() + "\n");
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
				}
			}
			while (result.HasFlag(ChangeStatus.Failed));
		}

		public override void UpdateCurrentStatus(Page page, string title)
		{
			// In theory, this could make use of a SectionedPage, but that seems a bit overkill for a simple log page.
			ThrowNull(page, nameof(page));
			ThrowNull(title, nameof(title));
			var sectionTitle = CurrentTaskFinder.Match(page.Text);
			if (!sectionTitle.Success)
			{
				throw new FormatException(BadLogPage);
			}

			var insertPos = sectionTitle.Index + sectionTitle.Length;
			sectionTitle = TaskLogFinder.Match(page.Text);
			page.Text = page.Text
				.Remove(insertPos, sectionTitle.Index - insertPos)
				.Insert(insertPos, title + "\n\n");
		}
		#endregion

		#region Private Static Methods
		private static Regex SectionFinder(string sectionName) => new Regex(@"^==\s*" + Regex.Escape(sectionName) + @"\s*==\s*?\n", RegexOptions.Multiline);

		private static string UniversalNow() => DateTime.UtcNow.ToString("u").TrimEnd('Z');
		#endregion
	}
}
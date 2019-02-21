namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiClasses;
	using static RobinHood70.WikiCommon.Globals;
	using static Properties.Resources;

	public class HoodBotFunctions : UserFunctions
	{
		#region Private Constants
		private const string Timestamp = "{{subst:#time:Y-m-d H:i:s|{{subst:CURRENTTIMESTAMP}}}}";
		#endregion

		#region Static Fields
		private static readonly Regex CurrentTaskFinder = SectionFinder("Current Task");
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
				this.UpdateCurrentStatus(this.LogPage, info.Title);

				var section = new Regex(@"(?<=id=""EntryTable"".*)({{/Entry|\|})", RegexOptions.Singleline);
				var sectionTitle = section.Match(this.LogPage.Text);
				if (!sectionTitle.Success)
				{
					throw new FormatException(BadLogPage);
				}

				var insertPos = sectionTitle.Index;
				var template = new Template("/Entry")
				{
					{ "task", info.Title },
					{ "start", Timestamp }
				};
				if (!string.IsNullOrWhiteSpace(info.Details))
				{
					template.Add("info", info.Details);
				}

				this.LogPage.Text = this.LogPage.Text.Insert(insertPos, template.ToString() + "\n");
				try
				{
					result = this.LogPage.Save("Task Started", false);
				}
				catch (StopException)
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
		private static Regex SectionFinder(string sectionName) => new Regex(@"^==\s*" + Regex.Escape(sectionName) + @"\s*==\s*$", RegexOptions.Multiline);
		#endregion
	}
}
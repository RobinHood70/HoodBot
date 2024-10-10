namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Revert Bot Edits")]
	internal sealed class BotRevert(JobManager jobManager) : EditJob(jobManager)
	{
		#region Private Constants
		private const string EditSummary = "Revert Future/Lore Link edits";
		#endregion

		#region Fields
		private readonly SortedDictionary<Title, long> undos = [];
		#endregion

		#region Public Override Properties
		public override string LogName => "Bot Self-Revert";
		#endregion

		#region Protected OVerride Methods
		protected override string GetEditSummary(Page page) => EditSummary;

		protected override void LoadPages()
		{
			if (this.Site.User is null)
			{
				this.StatusWriteLine("Can't run job, not logged in.");
				return;
			}

			var books = new TitleCollection(this.Site);
			books.GetBacklinks("Template:Book Header", BacklinksTypes.EmbeddedIn);
			var minDate = new DateTime(2022, 8, 22, 0, 0, 0, DateTimeKind.Utc);
			var maxDate = new DateTime(2022, 8, 22, 23, 59, 59, DateTimeKind.Utc);
			var contributionRange = this.Site.User?.GetContributions(minDate, maxDate);
			if (contributionRange is null)
			{
				return;
			}

			foreach (var contribution in contributionRange)
			{
				if (books.Contains(contribution.Title))
				{
					this.undos.Add(contribution.Title, contribution.Id);
				}
			}
		}

		protected override void PageLoaded(Page page)
		{
			// TODO: Might be candidate for a new job type.
		}

		protected override void Main()
		{
			this.ProgressMaximum = this.undos.Count;
			foreach (var undo in this.undos)
			{
				try
				{
					this.Site.Undo(undo.Key, undo.Value, EditSummary);
				}
				catch (WikiException we) when (string.Equals(we.Code, "undofailure", StringComparison.Ordinal))
				{
					this.WriteLine($"* {SiteLink.ToText(undo.Key)} - {we.Info}");
				}
				catch (WikiException we) when (string.Equals(we.Code, "nosuchrevid", StringComparison.Ordinal))
				{
					this.WriteLine($"* {SiteLink.ToText(undo.Key)} - only one edit, can't undo");
				}

				this.Progress++;
			}
		}
		#endregion
	}
}
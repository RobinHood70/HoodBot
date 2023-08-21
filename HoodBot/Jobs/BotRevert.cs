namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;

	internal sealed class BotRevert : EditJob
	{
		#region Fields
		private readonly SortedDictionary<Title, long> undos = new();
		#endregion

		#region Constructors
		[JobInfo("Revert Bot Edits")]
		public BotRevert(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Bot Self-Revert";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Revert Future/Lore Link edits";
		#endregion

		#region Protected OVerride Methods
		protected override void LoadPages()
		{
			this.Site.User.PropertyThrowNull(nameof(this.Site), nameof(this.Site.User));

			var books = new TitleCollection(this.Site);
			books.GetBacklinks("Template:Book Header", BacklinksTypes.EmbeddedIn);
			var minDate = new DateTime(2022, 8, 22, 0, 0, 0, DateTimeKind.Utc);
			var maxDate = new DateTime(2022, 8, 22, 23, 59, 59, DateTimeKind.Utc);
			var contributionRange = this.Site.User.GetContributions(minDate, maxDate);
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
					this.Site.Undo(undo.Key, undo.Value, this.EditSummary);
				}
				catch (WikiException we) when (string.Equals(we.Code, "undofailure", StringComparison.Ordinal))
				{
					this.WriteLine($"* {undo.Key.AsLink()} - {we.Info}");
				}
				catch (WikiException we) when (string.Equals(we.Code, "nosuchrevid", StringComparison.Ordinal))
				{
					this.WriteLine($"* {undo.Key.AsLink()} - only one edit, can't undo");
				}

				this.Progress++;
			}
		}
		#endregion
	}
}

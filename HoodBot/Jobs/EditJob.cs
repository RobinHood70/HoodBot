namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class EditJob : WikiJob
	{
		#region Constructors
		protected EditJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: this(site, asyncInfo, null) => this.JobType = JobTypes.Read | JobTypes.Write;

		protected EditJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo, params WikiTask[]? tasks)
			: base(site, asyncInfo, tasks) => this.Pages = new PageCollection(site);
		#endregion

		#region Public Properties

		// Nearly all edit jobs act on a PageCollection, so we provide a preinitialized one here for convenience.
		public PageCollection Pages { get; }
		#endregion

		#region Protected Properties

		/// <summary>Gets or sets the edit conflict action.</summary>
		/// <value>The edit conflict action.</value>
		/// <remarks>During a SavePage, if an edit conflict occurs, the page will automatically be re-loaded and the method specified here will be executed.</remarks>
		protected Action<EditJob, Page>? EditConflictAction { get; set; } = null;
		#endregion

		#region Protected Methods
		protected void SavePage(Page page, string editSummary, bool isMinor)
		{
			ThrowNull(page, nameof(page));
			var saved = false;
			while (!saved)
			{
				try
				{
					page.Save(editSummary, isMinor);
					saved = true;
				}
				catch (EditConflictException)
				{
					if (this.EditConflictAction == null)
					{
						throw;
					}
					else
					{
						page.Load();
						this.EditConflictAction(this, page);
					}
				}
			}
		}
		#endregion
	}
}
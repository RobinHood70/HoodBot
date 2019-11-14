namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class EditJob : WikiJob
	{
		#region Constructors
		protected EditJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
				: this(site, asyncInfo, null)
		{
		}

		protected EditJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
				: base(site, asyncInfo, tasks) => this.Pages = new PageCollection(site);
		#endregion

		#region Public Properties

		// Nearly all edit jobs act on a PageCollection, so we provide a preinitialized one here for convenience.
		public PageCollection Pages { get; }
		#endregion

		#region Public Override Properties
		public override bool ReadOnly => false;
		#endregion

		#region Public Virtual Properties
		public virtual string LogDetails { get; protected set; }
		#endregion

		#region Public Abstract Properties
		public abstract string LogName { get; }
		#endregion

		#region Protected Properties

		/// <summary>Gets or sets the edit conflict action.</summary>
		/// <value>The edit conflict action.</value>
		/// <remarks>During a SavePage, if an edit conflict occurs, the page will automatically be re-loaded and the method specified here will be executed.</remarks>
		protected Action<EditJob, Page> EditConflictAction { get; set; } = null;
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

		#region Protected Override Methods
		protected override void OnCompleted()
		{
			this.StatusWriteLine("Ending Log Entry");
			this.Site.UserFunctions.EndLogEntry();
			base.OnCompleted();
		}

		protected override void OnStarted()
		{
			this.PrepareJob();
			base.OnStarted();
			this.StatusWriteLine("Adding Log Entry");
			this.Site.UserFunctions.AddLogEntry(new LogInfo(this.LogName ?? this.GetType().Name, this.LogDetails, this.ReadOnly));
		}
		#endregion

		#region Protected Abstract Methods

		// While this could be virtual as well, the number of jobs with an empty PrepareJob method will be vanishingly small, so made it abstract instead.
		protected abstract void PrepareJob();
		#endregion
	}
}
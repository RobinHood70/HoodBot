namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	public abstract class ParsedPageJob : EditJob
	{
		#region Constructors
		protected ParsedPageJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Properties
		protected bool MinorEdit { get; set; } = true;
		#endregion

		#region Protected Abstract Properties
		protected abstract string EditSummary { get; }
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Loading Pages");
			this.Pages.PageLoaded += this.ResultsPageLoaded;
			this.LoadPages();
			this.Pages.PageLoaded -= this.ResultsPageLoaded;
		}

		protected override void Main() => this.SavePages(this.EditSummary, this.MinorEdit, this.ResultsPageLoaded);
		#endregion

		#region Protected Abstract Methods
		protected abstract void LoadPages();

		protected abstract void ParseText(object sender, ContextualParser parsedPage);
		#endregion

		#region Protected Virtual Methods
		protected virtual void ResultsPageLoaded(object sender, Page page)
		{
			ContextualParser parsedPage = new(page);
			this.ParseText(sender, parsedPage);
			page.Text = parsedPage.ToRaw();
		}
		#endregion
	}
}
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
			this.BeforeLoadPages();

			this.StatusWriteLine("Loading Pages");
			this.Pages.PageLoaded += this.ResultsPageLoaded;
			this.LoadPages();
			this.Pages.PageLoaded -= this.ResultsPageLoaded;

			this.AfterLoadPages();
		}

		protected override void Main() => this.SavePages(this.EditSummary, this.MinorEdit, this.ResultsPageLoaded);
		#endregion

		#region Protected Abstract Methods
		protected abstract void LoadPages();

		protected abstract void ParseText(object sender, ContextualParser parser);
		#endregion

		#region Protected Virtual Methods
		protected virtual void AfterLoadPages()
		{
		}

		protected virtual void BeforeLoadPages()
		{
		}

		protected virtual void FillPage(Page page)
		{
		}

		protected virtual void ResultsPageLoaded(object sender, Page page)
		{
			if (page.IsMissing || page.Text.Trim().Length == 0)
			{
				this.FillPage(page);
			}

			ContextualParser parser = new(page);
			this.ParseText(sender, parser);
			parser.UpdatePage();
		}
		#endregion
	}
}
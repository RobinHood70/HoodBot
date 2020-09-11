namespace RobinHood70.HoodBot.Jobs.Design
{
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.ContextualParser;

	public abstract class ParsedPageJob : EditJob
	{
		#region Constructors
		protected ParsedPageJob([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Abstract Properties
		protected abstract string EditSummary { get; }
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Loading Pages");
			this.Pages.PageLoaded += this.Results_PageLoaded;
			this.LoadPages();
			this.Pages.PageLoaded -= this.Results_PageLoaded;
		}

		protected override void Main() => this.SavePages(this.EditSummary, true, this.Results_PageLoaded);
		#endregion

		#region Protected Abstract Methods
		protected abstract void LoadPages();

		protected abstract void ParseText(object sender, Parser parsedPage);
		#endregion

		#region Private Methods
		private void Results_PageLoaded(object sender, Page eventArgs)
		{
			var parsedPage = new Parser(eventArgs);
			this.ParseText(sender, parsedPage);
			eventArgs.Text = parsedPage.GetText();
		}
		#endregion
	}
}
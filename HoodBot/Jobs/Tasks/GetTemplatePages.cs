namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	internal class GetTemplatePages : WikiTask
	{
		#region Fields
		private readonly bool respectRedirects;
		private readonly PageCollection result;
		private readonly TitleCollection templates;
		#endregion

		#region Constructors
		public GetTemplatePages(WikiRunner parent, TitleCollection templates, bool respectRedirects, PageCollection result)
			: base(parent)
		{
			this.respectRedirects = respectRedirects;
			this.result = result;
			this.templates = templates;
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.StatusWriteLine("Loading pages");
			this.ProgressMaximum = this.templates.Count;
			if (this.templates.Count == 1)
			{
				this.result.AddBacklinks(this.templates[0].FullPageName, BacklinksTypes.EmbeddedIn, this.respectRedirects);
			}
			else
			{
				this.ProgressMaximum += this.templates.Count;

				// Pages for multiple templates could overlap significantly, so figure out which pages to load first, then load by title.
				var fullList = new TitleCollection(this.Site);
				foreach (var title in this.templates)
				{
					fullList.AddBacklinks(title.FullPageName, BacklinksTypes.EmbeddedIn, this.respectRedirects);
					this.IncrementProgress();
				}

				this.result.AddTitles(fullList);
			}

			this.result.Sort();
			this.IncrementProgress();
		}
		#endregion
	}
}
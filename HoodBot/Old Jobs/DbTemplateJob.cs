namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Template Job")]
	public abstract class DbTemplateJob<TKey, TItem>(JobManager jobManager) : EditJob(jobManager)
		where TKey : notnull
	{
		#region Protected Properties
		protected IDictionary<TKey, TItem> DbItems { get; } = new Dictionary<TKey, TItem>();
		#endregion

		#region Protected Abstract Properties
		protected abstract string ConnectionString { get; }

		protected abstract string Query { get; }

		protected abstract string TemplateName { get; }
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			foreach (var item in this.DbItems)
			{
				var page = this.GetPageForItem(item);
				var parser = new SiteParser(page);
				var template = this.GetTemplateFromParser(parser);
				this.UpdateTemplateFromItem(template, item);
				parser.UpdatePage();
			}
		}

		protected override void BeforeLoadPages()
		{
			this.StatusWriteLine("Loading items from database");
			foreach (var value in Database.RunQuery(this.ConnectionString, this.Query, this.NewItem))
			{
				this.DbItems.Add(this.GetKeyForItem(value), value);
			}
		}

		protected override void LoadPages()
		{
			var title = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], this.TemplateName);
			this.Pages.GetBacklinks(title.FullPageName(), BacklinksTypes.EmbeddedIn);
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract TKey GetKeyForItem(TItem item);

		protected abstract Page GetPageForItem(KeyValuePair<TKey, TItem> item);

		protected abstract ITemplateNode GetTemplateFromParser(SiteParser parser);

		protected abstract TItem NewItem(IDataRecord record);

		protected abstract Page NewPage(Title pageName);

		protected abstract void ParseTemplate(ITemplateNode template, SiteParser parser);

		protected abstract void UpdateTemplateFromItem(ITemplateNode template, KeyValuePair<TKey, TItem> item);
		#endregion
	}
}
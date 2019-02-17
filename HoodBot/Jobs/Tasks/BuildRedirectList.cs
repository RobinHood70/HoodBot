namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class BuildRedirectList : WikiTask
	{
		private readonly TitleCollection originalPages;
		private readonly TitleCollection result;

		public BuildRedirectList(WikiRunner parent, TitleCollection pageNames, TitleCollection result)
			: base(parent)
		{
			this.originalPages = pageNames;
			this.result = result;
		}

		protected override void Main()
		{
			var originalFollowed = this.originalPages.Load(new PageLoadOptions(PageModules.None) { FollowRedirects = true });
			this.result.AddCopy(originalFollowed);
			foreach (var page in originalFollowed.TitleMap)
			{
				var title = new TitleParts(this.Site, page.Key);
				if (title.IsLocalWiki)
				{
					this.result.Add(new Title(title));
				}
			}

			// Loop until nothing new is added.
			var pagesToCheck = new HashSet<Title>(this.result, new SimpleTitleEqualityComparer());
			var alreadyChecked = new HashSet<Title>(new SimpleTitleEqualityComparer());
			do
			{
				foreach (var page in pagesToCheck)
				{
					this.result.AddBacklinks(page.FullPageName, WikiCommon.BacklinksTypes.Backlinks, true, WikiCommon.Filter.Only);
				}

				alreadyChecked.UnionWith(pagesToCheck);
				pagesToCheck.Clear();
				pagesToCheck.UnionWith(this.result);
				pagesToCheck.ExceptWith(alreadyChecked);
			}
			while (pagesToCheck.Count > 0);
		}
	}
}

namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class BuildRedirectList : WikiTask
	{
		private readonly TitleCollection original;
		private readonly TitleCollection result;

		public BuildRedirectList(WikiRunner parent, TitleCollection pageNames, TitleCollection result)
			: base(parent)
		{
			this.original = pageNames;
			this.result = result;
		}

		protected override void Main()
		{
			var originalFollowed = this.original.Load(new PageLoadOptions(PageModules.None) { FollowRedirects = true });
			var newSet = new HashSet<Title>(new SimpleTitleEqualityComparer());
			this.original.Clear();
			foreach (var page in originalFollowed)
			{
				var title = new Title(page);
				this.original.Add(page);
				this.result.Add(page);
				newSet.Add(title);
			}

			foreach (var page in originalFollowed.TitleMap)
			{
				var title = new TitleParts(this.Site, page.Key);
				if (title.IsLocalWiki)
				{
					newSet.Add(new Title(title));
				}
			}

			// Loop until nothing new is added.
			while (newSet.Count > 0)
			{
				newSet.ExceptWith(this.result);
				var redirects = new List<Title>();
				foreach (var page in newSet)
				{
					this.result.Add(page);
					var newCollection = TitleCollection.CopyFrom(newSet);
					newCollection.AddBacklinks(page.FullPageName, WikiCommon.BacklinksTypes.Backlinks, true, WikiCommon.Filter.Only);
					foreach (var item in newCollection)
					{
						redirects.Add(new Title(item));
					}
				}

				newSet.Clear();
				newSet.UnionWith(redirects);
			}
		}
	}
}

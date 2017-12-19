namespace RobinHood70.Robby
{
	using System.Collections.Generic;
	using Design;
	using Pages;
	using WallE.Base;
	using static WikiCommon.Globals;

	public class PageBuilder : PageBuilderBase
	{
		#region Public Override Methods
		public override Page CreatePage(Site site, int ns, string title)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(title, nameof(title));
			switch (ns)
			{
				case MediaWikiNamespaces.MediaWiki:
					return new Message(site, title);
				case MediaWikiNamespaces.File:
					return new FilePage(site, title);
				case MediaWikiNamespaces.Category:
					return new Category(site, title);
			}

			return new Page(site, title);
		}

		public override PageItem CreatePageItem() => new PageItem();
		#endregion

		#region Protected Override Methods
		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
		}

		protected override void PopulateCustom(Page page, PageItem pageItem)
		{
		}
		#endregion
	}
}
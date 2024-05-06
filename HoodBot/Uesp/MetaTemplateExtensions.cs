namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	internal static class MetaTemplateExtensions
	{
		public static PageCollection CreateMetaPageCollection(this Site site, PageModules pageModules, bool followRedirects, params string[] variables) => CreateMetaPageCollection(site, pageModules, followRedirects, true, variables);

		public static PageCollection CreateMetaPageCollection(this Site site, PageModules pageModules, bool followRedirects, bool gameSpaceOnly, params string[] variables)
		{
			ArgumentNullException.ThrowIfNull(site);
			PageLoadOptions pageLoadOptions = new(pageModules | PageModules.Custom, followRedirects)
			{
				PageCreator = new MetaTemplateCreator(site.DefaultLoadOptions.PageCreator, variables)
				{
					GameSpaceOnly = gameSpaceOnly
				}
			};

			return new PageCollection(site, pageLoadOptions);
		}
	}
}
namespace RobinHood70.HoodBot.Uesp
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	internal static class MetaTemplateExtensions
	{
		public static PageCollection CreateMetaPageCollection(this Site site, PageLoadOptions pageLoadOptions, params string[] variables)
		{
			var pageCreator = new MetaTemplateCreator(variables);
			return new PageCollection(site.NotNull(nameof(site)), pageLoadOptions, pageCreator);
		}

		public static PageCollection CreateMetaPageCollection(this Site site, PageModules pageModules, bool followRedirects, params string[] variables)
		{
			var pageLoadOptions = new PageLoadOptions(pageModules | PageModules.Custom, followRedirects);
			return CreateMetaPageCollection(site.NotNull(nameof(site)), pageLoadOptions, variables);
		}
	}
}

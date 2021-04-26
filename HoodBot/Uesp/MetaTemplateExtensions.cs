namespace RobinHood70.HoodBot.Uesp
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	internal static class MetaTemplateExtensions
	{
		public static PageCollection CreateMetaPageCollection(this Site site, PageLoadOptions pageLoadOptions, params string[] variables)
		{
			ThrowNull(site, nameof(site));
			var pageCreator = new MetaTemplateCreator(variables);
			return new PageCollection(site, pageLoadOptions, pageCreator);
		}

		public static PageCollection CreateMetaPageCollection(this Site site, PageModules pageModules, bool followRedirects, params string[] variables)
		{
			ThrowNull(site, nameof(site));
			var pageLoadOptions = new PageLoadOptions(pageModules | PageModules.Custom, followRedirects);
			return CreateMetaPageCollection(site, pageLoadOptions, variables);
		}
	}
}

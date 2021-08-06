namespace RobinHood70.HoodBot.Uesp
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	internal static class MetaTemplateExtensions
	{
		public static PageCollection CreateMetaPageCollection(this Site site, PageModules pageModules, bool followRedirects, params string[] variables)
		{
			PageLoadOptions pageLoadOptions = new(pageModules | PageModules.Custom, followRedirects)
			{
				PageCreator = new MetaTemplateCreator(site.DefaultLoadOptions.PageCreator, variables)
			};

			return new PageCollection(site.NotNull(nameof(site)), pageLoadOptions);
		}
	}
}

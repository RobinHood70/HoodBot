namespace HoodBotTestProject
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Test;
	using RobinHood70.WikiCommon;
	using Xunit;

	public class TitleParserUnitTest
	{
		#region Fields
		private static readonly Site Site = new Site(new WikiAbstractionLayer());
		#endregion

		#region Static Constructor
		static TitleParserUnitTest() => Site.Login("RobinHood70", string.Empty);
		#endregion

		#region Public Static Properties
		public static TheoryData<int, string, string?, int, string> CoercionTestData =>
			new TheoryData<int, string, string?, int, string>
			{
				{ MediaWikiNamespaces.Main, "Test", null, MediaWikiNamespaces.Main, "Test" },
				{ MediaWikiNamespaces.Category, "Test", null, MediaWikiNamespaces.Main, "Test" },
				{ MediaWikiNamespaces.Main, "File:Test.png", null, MediaWikiNamespaces.File, "Test.png" },
				{ MediaWikiNamespaces.File, "File:Test.png", null, MediaWikiNamespaces.File, "Test.png" },
				{ MediaWikiNamespaces.User, "File:Test.png", null, MediaWikiNamespaces.File, "Test.png" },
				{ MediaWikiNamespaces.Main, "en:Image:Test.png", "en", MediaWikiNamespaces.File, "Test.png" },
				{ MediaWikiNamespaces.File, "en:Image:Test.png", "en", MediaWikiNamespaces.File, "Test.png" },
				{ MediaWikiNamespaces.User, "en:Image:Test.png", "en", MediaWikiNamespaces.File, "Test.png" },
				{ MediaWikiNamespaces.User, "mediawikiwiki:Image:Test.png", "mediawikiwiki", MediaWikiNamespaces.Main, "Image:Test.png" },
			};

		public static TheoryData<string, string?, int, string, string?> TestData =>
			new TheoryData<string, string?, int, string, string?>
			{
				{ "Test", null, MediaWikiNamespaces.Main, "Test", null },
				{ "File:Test.png", null, MediaWikiNamespaces.File, "Test.png", null },
				{ "Image:Test.png", null, MediaWikiNamespaces.File, "Test.png", null },
				{ "Talk:File:Test.png", null, MediaWikiNamespaces.Talk, "File:Test.png", null },
				{ "Unknown:Page", null, MediaWikiNamespaces.Main, "Unknown:Page", null },
				{ "en", null, MediaWikiNamespaces.Main, "En", null },
				{ ":en", null, MediaWikiNamespaces.Main, "En", null },
				{ ":en:", "en", MediaWikiNamespaces.Main, "Main Page", null },
				{ "en:Test", "en", MediaWikiNamespaces.Main, "Test", null },
				{ "en:Image:Test.png", "en", MediaWikiNamespaces.File, "Test.png", null },
				{ ":en::Image:Test.png#Everything", "en", MediaWikiNamespaces.Main, ":Image:Test.png", "Everything" },
				{ "MediaWikiWiki:File:Test.png", "mediawikiwiki", MediaWikiNamespaces.Main, "File:Test.png", null },
				{ "Category:Marked for Deletion", null, MediaWikiNamespaces.Category, "Marked for Deletion", null },
			};
		#endregion

		#region Public Methods
		[Theory]
		[MemberData(nameof(TestData))]
		public void Test1(string input, string? iw, int ns, string page, string? fragment)
		{
			var test = new TitleParser(Site, input);
			Assert.Equal(iw, test.Interwiki?.Prefix);
			Assert.Equal(ns, test.Namespace.Id);
			Assert.Equal(page, test.PageName);
			Assert.Equal(fragment, test.Fragment);
		}

		[Theory]
		[MemberData(nameof(CoercionTestData))]
		public void Test2(int nsInput, string nameInput, string? iw, int ns, string page)
		{
			var test = new TitleParser(Site, nsInput, nameInput, false);
			Assert.Equal(iw, test.Interwiki?.Prefix);
			Assert.Equal(ns, test.Namespace.Id);
			Assert.Equal(page, test.PageName);
		}
		#endregion
	}
}
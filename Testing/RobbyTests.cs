namespace RobinHood70.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Testing.MetaTemplate;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon;
	using static RobinHood70.Testing.TestingCommon;

	public class RobbyTests : TestRunner, ITestRunner
	{
		#region Fields
		private Site adminWiki;
		private Site normalWiki;
		#endregion

		#region Constructors
		public RobbyTests(ITestForm parentForm, WikiInfo wikiInfo)
			: base(parentForm, wikiInfo)
		{
		}
		#endregion

		#region Public Static Methods
		public static void DumpTitles(IEnumerable<Title> titles)
		{
#if DEBUG
			var count = 0;
			foreach (var entry in titles)
			{
				Debug.WriteLine(entry.FullPageName);
				count++;
			}

			Debug.WriteLine(count);
#endif
		}
		#endregion

		#region Public Override Methods
		public override void RunOne() => this.TitlePartsTests();

		public override void RunAll()
		{
			if (this.normalWiki.ServerName == "rob-centos")
			{
				// TODO: Separate out the ones that should in fact only be run on my server.
				this.AllMessagesTest();
				this.BacklinksTests();
				this.BlocksTests();
				this.CategoryMembersTests();
				this.CategoryTests();
				this.DeleteTests();
				this.DuplicateFilesTests();
				this.FileUsagesTests();
				this.MetaTemplateTests();
				this.MoveTests();
				this.NamespaceTests();
				this.PageCollectionFromCategoriesTest();
				this.PageCollectionFromQueryPage();
				this.PagesCategoriesOnTests();
				this.PageTests();
				this.PageTypeTests();
				this.ProtectTests();
				this.ProtectedTitlesTests();
				this.PurgeTests();
				this.RecentChangesTests();
				this.RedirectTargetTests();
				this.SearchTests();
				this.TemplateTransclusionTest();
				this.TitleTests();
				this.TitlesAllPagesTests();
				this.UnwatchTests();
				this.UploadRandomImageTest();
				this.UserBlockTests();
				this.UserContributionsTests();
				this.UserEmailTests();
				this.UserFullInfoTests();
				this.UserMessageTests();
				this.UsersTests();
				this.UserWatchlistTests();
				this.WatchTests();
			}
		}

		public override void Setup()
		{
			this.normalWiki = GetSite(this.WikiInfo, false);
			var wal = this.normalWiki.AbstractionLayer as WikiAbstractionLayer;
			if (wal.Uri.Host.Contains("uesp.net"))
			{
				wal.ModuleFactory.RegisterProperty<VariablesInput>(PropVariables.CreateInstance);
				wal.ModuleFactory.RegisterGenerator<VariablesInput>(PropVariables.CreateInstance);
			}

			if (this.WikiInfo.AdminUserName != null)
			{
				this.adminWiki = GetSite(this.WikiInfo, true);
			}
		}

		public override void Teardown()
		{
			if (this.adminWiki != null && !string.IsNullOrEmpty(this.WikiInfo.SecretKey))
			{
				RunJobs(this.adminWiki.AbstractionLayer as WikiAbstractionLayer, this.WikiInfo.SecretKey);
			}

			TeardownSite(this.normalWiki);
			this.normalWiki = null;

			TeardownSite(this.adminWiki);
			this.adminWiki = null;
		}
		#endregion

		#region Tests
		internal void AllMessagesTest()
		{
			var titles = new TitleCollection(this.normalWiki);
			titles.GetMessages(Filter.Only);
			DumpTitles(titles);
		}

		internal void BacklinksTests()
		{
			var titles = new TitleCollection(this.normalWiki);
			titles.GetBacklinks("Oblivion:Oblivion", BacklinksTypes.Backlinks | BacklinksTypes.EmbeddedIn, true, Filter.Any, MediaWikiNamespaces.Template);
			this.CheckCollection(titles, "Backlinks");
			DumpTitles(titles);
		}

		internal void BlocksTests()
		{
			var result = this.normalWiki.LoadBlocks(new[] { "RobinHood70", "HoodBot", "HotnBOThered", "Dagoth Ur" });
			foreach (var item in result)
			{
				var expiry = item.Expiry == DateTime.MaxValue ? "indefinite" : item.Expiry.ToString();
				Debug.WriteLine($"{item.User} blocked by {item.BlockedBy} on {item.StartTime}. Expires: {expiry}");
			}

			result = this.normalWiki.LoadBlocks(Filter.Only, Filter.Any, Filter.Exclude, Filter.Any);
			var set = new HashSet<string>();
			foreach (var item in result)
			{
				set.Add(item.User);
			}

			var list = new List<string>(set);
			list.Sort();
			foreach (var item in list)
			{
				Debug.WriteLine(item);
			}
		}

		internal void CategoryMembersTests()
		{
			var titles = new PageCollection(this.normalWiki);
			titles.GetCategoryMembers("Books-Images", CategoryMemberTypes.File, true);
			this.CheckCollection(titles, "CategoryMembers");
			DumpTitles(titles);
		}

		internal void CategoryTests()
		{
			var titles = new TitleCollection(this.normalWiki);
			titles.GetCategories("Arena-A", "Arena-J");
			DumpTitles(titles);
		}

		internal void DeleteTests()
		{
			var page = new Page(this.adminWiki, "Delete Test")
			{
				Text = "Test page to be deleted."
			};
			page.Save("Create test page", false);
			page.Delete("Delete test page");
		}

		internal void DuplicateFilesTests()
		{
			const string duped = "File:ON-icon-ava-Defensive Scroll Bonus I.png";
			var pageCollection = new PageCollection(this.normalWiki);
			pageCollection.GetDuplicateFiles(new TitleCollection(this.normalWiki, duped));
			DumpTitles(pageCollection);

			var filePage = new FilePage(this.normalWiki, duped);
			var files = filePage.FindDuplicateFiles();
			DumpTitles(files);
		}

		internal void FileUsagesTests()
		{
			const string used = "File:EnwiktwatchlistCapture.PNG";
			var filePage = new FilePage(this.normalWiki, used);
			var result = filePage.FileUsage();
			var files = TitleCollection.CopyFrom(result);
			DumpTitles(files);
		}

		internal void MetaTemplateTests()
		{
			this.normalWiki.DefaultLoadOptions = new PageLoadOptions(PageModules.Info | PageModules.Revisions | PageModules.Custom);
			var titles = new TitleCollection(this.normalWiki, "Legends:Adoring Fan");
			var pages = titles.Load();
			foreach (var page in pages)
			{
				var metaPage = page as VariablesPage;
				Debug.WriteLine(metaPage.PageName);
				foreach (var metavarSet in metaPage.VariableSets)
				{
					Debug.WriteLine("Subset: " + metavarSet.Key ?? "<none>");
					foreach (var metavar in metavarSet.Value)
					{
						Debug.WriteLine($"  {metavar.Key} = {metavar.Value}");
					}
				}
			}
		}

		internal void MoveTests()
		{
			var page = new Page(this.adminWiki, "Move Wrong Page Test")
			{
				Text = "Test page to be moved " + DateTime.UtcNow.ToString()
			};
			page.Save("Create test page", false);

			Debug.WriteLine(page.Move("Move Test", "Move test page", true, out var moveResults));
			foreach (var move in moveResults)
			{
				Debug.WriteLine(move.Key, move.Value);
			}
		}

		internal void NamespaceTests()
		{
			var nss = this.normalWiki.Namespaces;
			this.Assert(nss["template"].Id == MediaWikiNamespaces.Template, "String indexing not working.");
			this.Assert(nss[0] == nss[MediaWikiNamespaces.Main] && nss[0] == nss[string.Empty], "Equivalent namespaces aren't.");
			this.Assert(nss[MediaWikiNamespaces.File].Contains("Image"), "Namespace.Contains failed.");
			this.Assert(nss[MediaWikiNamespaces.Template] == MediaWikiNamespaces.Template, "Namespace equals integer failed.");

			nss.AddToNames("Main", this.normalWiki.Namespaces[MediaWikiNamespaces.Main]);
			this.Assert(nss["main"].Id == MediaWikiNamespaces.Main, "Main namespace does not appear to have been added.");
		}

		internal void PageCollectionFromCategoriesTest()
		{
			var sourcePages = new TitleCollection(this.normalWiki, "Main Page");
			var pageCollection = new PageCollection(this.normalWiki);
			pageCollection.GetPageCategories(sourcePages);
			foreach (var page in pageCollection)
			{
				this.Assert(page.Namespace.Id == MediaWikiNamespaces.Category, "A page in the returned collection isn't a category.");
			}
		}

		internal void PagesCategoriesOnTests()
		{
			var pages = new PageCollection(this.normalWiki) { LoadOptions = PageLoadOptions.None };
			var categoryTitles = new TitleCollection(this.normalWiki, "API:Categories", "API:Purge");
			pages.GetPageCategories(categoryTitles, Filter.Any);
			DumpTitles(pages);
		}

		internal void PageTests()
		{
			var pages = new PageCollection(this.normalWiki);
			pages.AddTitles("MediaWiki:1movedto2");
			foreach (var page in pages)
			{
				Debug.WriteLine($"Invalid: {page.Invalid}; Missing: {page.Missing}; Text: {page.Text}");
			}

			this.Assert(Page.CheckExistence(this.normalWiki, "Main Page"), "Main Page not detected as existing.");
			this.Assert(!Page.CheckExistence(this.normalWiki, "This page does not exist"), "Non-existent page detected as existing.");
			this.Assert(new Title(this.normalWiki, "Template:Test").KeyedEquals(new Page(this.normalWiki, "Template:Test")), "Title and Page should be equal, but aren't.");
		}

		internal void PageTypeTests()
		{
			var loadOptions = new PageLoadOptions(PageModules.All) { FileRevisionCount = 5 };
			var pageCollection = new PageCollection(this.normalWiki, loadOptions);
			pageCollection.AddTitles("Category:All Pages Missing Data", "Category:Categories", "Oblivion:Oblivion", "File:ZeniMax Online Studios logo.jpg");
			foreach (var page in pageCollection)
			{
				Debug.Write(page.FullPageName + " is a ");
				if (page is FilePage fp)
				{
					Debug.WriteLine($"file page. ");
					foreach (var fileRevision in fp.FileRevisions)
					{
						Debug.WriteLine($"  Image Dimensions = {fileRevision.Width} x {fileRevision.Height}, Size = {fileRevision.Size}");
					}
				}
				else if (page is Category cp)
				{
					Debug.WriteLine($"category page. Hidden: {cp.Hidden}, Total items: {cp.FullCount}");
				}
				else
				{
					Debug.WriteLine("regular page.");
				}
			}
		}

		internal void ProtectTests()
		{
			var title = new Title(this.adminWiki, "Create Protection Test");
			title.CreateProtect("Test create protection", ProtectionLevel.Full, DateTime.Now + new TimeSpan(0, 1, 0));
			title.CreateUnprotect("Test create unprotection");

			var page = new Page(this.adminWiki, "Protection Test Page")
			{
				Text = "Protection test page: " + DateTime.UtcNow.ToString()
			};
			page.Save("Create test page", false);
			page.Protect("Test protection", ProtectionLevel.Full, ProtectionLevel.Semi, null);
			page.Unprotect("Test unprotection", true, true);
		}

		internal void ProtectedTitlesTests()
		{
			var titles = new TitleCollection(this.normalWiki);
			titles.GetProtectedTitles();
			this.CheckCollection(titles, "ProtectedTitles");
			DumpTitles(titles);
		}

		internal void PurgeTests()
		{
			var titles = new TitleCollection(this.normalWiki, "User:RobinHood70");
			var result = titles.Purge(PurgeMethod.Normal, out var purgeResults);
			Debug.WriteLine(result);
			DumpTitles(purgeResults);
		}

		internal void RecentChangesTests()
		{
			var result = this.normalWiki.LoadRecentChanges();
			Debug.WriteLine(result.Count);
		}

		internal void RedirectTargetTests()
		{
			var target = this.normalWiki.GetRedirectFromText("#REDIRECT [[Template:Hello]]");
			this.Assert(target.FullPageName == "Template:Hello", "Incorrect template target.");

			target = this.normalWiki.GetRedirectFromText("#WEITERLEITUNG [[Template:Hello|Stupid text]]][[Flower]]");
			this.Assert(target.FullPageName == "Template:Hello", "Incorrect template target.");

			target = this.normalWiki.GetRedirectFromText(" #REDIRECT [[Hello world]]");
			this.Assert(target != null, "Incorrectly detected a malformed redirect.");
		}

		internal void SearchTests()
		{
			var titles = new TitleCollection(this.normalWiki);
			titles.GetSearchResults("aleph", WhatToSearch.Title, this.normalWiki.Namespaces.RegularIds);
			DumpTitles(titles);
		}

		internal void TemplateTransclusionTest()
		{
			var titleCollection = new TitleCollection(this.normalWiki);
			titleCollection.GetTransclusions();
			DumpTitles(titleCollection);
		}

		internal void TitleTests()
		{
			this.Assert(Title.PipeTrick("(Test)") == "(Test)", "PipeTrick failed for (Test).");
			this.Assert(Title.PipeTrick("Hello (Test)") == "Hello", "PipeTrick failed for Hello (Test).");
			this.Assert(Title.PipeTrick("Hello (Test), Goodbye") == "Hello", "PipeTrick failed for Hello (Test), Goodbye.");
			this.Assert(Title.PipeTrick("Hello, Goodbye (Test)") == "Hello, Goodbye", "PipeTrick failed for Hello, Goodbye (Test).");
			this.Assert(Title.NameFromParts(this.normalWiki.Namespaces[MediaWikiNamespaces.Template], "!", null) == "Template:!", "NameFromParts failed for Template:!");
			this.Assert(Title.NameFromParts(this.normalWiki.Namespaces[MediaWikiNamespaces.Main], "Main Page", "Test") == "Main Page#Test", "NameFromParts failed for Main Page#Test.");

			var title = new Title(this.normalWiki, "Template:!");
			this.Assert(title.Namespace.Id == MediaWikiNamespaces.Template, "Namespace was incorrect for Template:!.");
			this.Assert(title.PageName == "!", "PageName was incorrect for Template:!.");
			this.Assert(title.SubjectPage.FullPageName == "Template:!", "SubjectPage was incorrect for Template:!.");
			this.Assert(title.TalkPage.FullPageName == "Template talk:!", "TalkPage was incorrect for Template:!.");
		}

		internal void TitlePartsTests()
		{
			this.Assert(TitleParts.DecodeAndNormalize("Hello\u200E\u200F\u202A\u202B\u202C\u202D\u202E_\xA0\u1680\u180E\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u2028\u2029\u202F\u205F\u3000&amp;World") == "Hello                    &World", "Text was not fully stripped and/or replaced.");
			var title = new TitleParts(this.normalWiki, ":eN:sKyRiM:skyrim#Modding");
			this.Assert(title.Interwiki.Prefix == "en", "Incorrect interwiki");
			this.Assert(title.Namespace.Name == "Skyrim", "Incorrect namespace");
			this.Assert(title.PageName == "Skyrim", "Incorrect pagename");
			this.Assert(title.Fragment == "Modding", "Incorrect fragment");
			this.Assert(title.FullPageName == "Skyrim:Skyrim", "Incorrect full page name");

			var caught = false;
			try
			{
				title = new TitleParts(this.normalWiki, "Talk:File:Test.jpg");
			}
			catch (ArgumentException)
			{
				caught = true;
			}

			this.Assert(caught, "Error not caught");
		}

		internal void TitlesAllPagesTests()
		{
			var titles = new TitleCollection(this.normalWiki);
			var sw = new Stopwatch();
			sw.Start();
			titles.GetNamespace(MediaWikiNamespaces.Template, Filter.Any, "A", "C");
			Debug.WriteLine("Count: " + titles.Count);
			titles.Clear();
			titles.GetNamespace(MediaWikiNamespaces.Template, Filter.Any, "A", "B");
			Debug.WriteLine("Count: " + titles.Count);
			titles.GetNamespace(MediaWikiNamespaces.Template, Filter.Any, "A", "C");
			Debug.WriteLine("Count: " + titles.Count);
			Debug.WriteLine("Time: " + sw.ElapsedMilliseconds);
			foreach (var title in titles)
			{
				Debug.WriteLine(title.PageName);
			}
		}

		internal void UnwatchTests()
		{
			var titles = new TitleCollection(this.normalWiki, "User:RobinHood70");
			var result = titles.Unwatch(out var unwatchResult);
			Debug.WriteLine(result);
			DumpTitles(unwatchResult);
		}

		internal void UploadRandomImageTest()
		{
			var rand = new Random();
			var files = Directory.GetFiles(@"C:\Users\rmorl\Pictures\Screen Saver Pics\", "*.jpg"); // Only select from jpgs so we don't have to worry about extension type.
			var fileName = files[rand.Next(files.Length)];
			this.normalWiki.Upload(fileName, "Test Image.jpg", "Test upload");
		}

		internal void UserBlockTests()
		{
			var user = new User(this.adminWiki, "Test User");
			user.Block("Because he's a bad person", BlockFlags.AutoBlock | BlockFlags.AllowUserTalk, "5 minutes", true);
			user.Unblock("Because he's a good person");
		}

		internal void UserContributionsTests()
		{
			var user = new User(this.normalWiki, "RobinHood70");
			var result = user.GetContributions();
			var titles = new HashSet<string>();
			foreach (var item in result)
			{
				titles.Add(item.Title.FullPageName);
			}

			Debug.WriteLine($"{result.Count} contributions on {titles.Count} pages");
		}

		internal void UserEmailTests()
		{
			var user = new User(this.normalWiki, "RobinHood70");
			var result = user.Email("This is a test e-mail.", true, out var message);
			Debug.WriteLine(result);
			Debug.WriteLine(message);
		}

		internal void UserFullInfoTests()
		{
			var userLoad = new User(this.normalWiki, "RobinHood70");
			userLoad.Load();
			Debug.WriteLine(string.Join(",", userLoad.Groups));
			Debug.WriteLine(userLoad.Gender);

			var users = this.normalWiki.LoadUserInformation("RobinHood70", "Test User");
			foreach (var user in users)
			{
				Debug.Write('\n');
				Debug.WriteLine(user.Name);
				Debug.WriteLine(string.Join(",", user.Groups));
				Debug.WriteLine(user.Gender);
			}
		}

		internal void UserMessageTests()
		{
			var user = new User(this.normalWiki, "RobinHood70");
			user.NewTalkPageMessage("Test Message", "Hi there!", "Create a test message.");
		}

		internal void UsersTests()
		{
			Debug.WriteLine("Active Users: {0}", this.normalWiki.LoadUsers(true, false).Count);
			Debug.WriteLine("Sysops: {0}", this.normalWiki.LoadUsersInGroups(false, false, "sysop").Count);
			Debug.WriteLine("API High Limits: {0}", this.normalWiki.LoadUsersWithRights(false, false, "apihighlimits").Count);
			Debug.WriteLine("API High Limits with edits: {0}", this.normalWiki.LoadUsersWithRights(false, true, "apihighlimits").Count);
			Debug.WriteLine("API High Limits that are active: {0}", this.normalWiki.LoadUsersWithRights(true, false, "apihighlimits").Count);
		}

		internal void UserWatchlistTests()
		{
			var user = new User(this.adminWiki, "RobinHood70");
			var result = user.GetWatchlist("625ff6a84caa118b313fab35cbdf75f356cb93b8", this.normalWiki.Namespaces.RegularIds);
			Debug.WriteLine($"RobinHood70 has {result.Count} pages in his watchlist.");
		}

		internal void WatchTests()
		{
			var titles = new TitleCollection(this.normalWiki, "User:RobinHood70");
			var result = titles.Watch(out var watchResult);
			Debug.WriteLine(result);
			DumpTitles(watchResult);
		}
		#endregion

		#region Private Static Methods
		private static Site GetSite(WikiInfo info, bool useAdmin)
		{
			var wal = GetAbstractionLayer(info, useAdmin);
			var site = new Site(wal);
			site.Login(useAdmin ? info.AdminUserName : info.UserName, useAdmin ? info.AdminPassword : info.Password);
#if DEBUG
			wal.SendingRequest += DebugShowRequest;
			// wal.WarningOccurred += DebugWarningEventHandler;
			// wal.ResponseReceived += DebugResponseEventHandler;
			site.WarningOccurred += DebugWarningEventHandler;
#endif

			return site;
		}

		private static void TeardownSite(Site site)
		{
#if DEBUG
			site.WarningOccurred -= DebugWarningEventHandler;

			var wal = site.AbstractionLayer as WikiAbstractionLayer;
			// wal.ResponseReceived -= DebugResponseEventHandler;
			// wal.WarningOccurred -= DebugWarningEventHandler;
			wal.SendingRequest -= DebugShowRequest;
#endif
		}
		#endregion

		#region
		private void PageCollectionFromQueryPage()
		{
			var pageCollection = new PageCollection(this.normalWiki);
			pageCollection.GetQueryPage("Mostlinked");
			DumpTitles(pageCollection);
		}
		#endregion
	}
}

namespace RobinHood70.Robby.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Security.Cryptography;
	using System.Text;
	using System.Windows.Forms;
	using Design;
	using Pages;
	using Robby;
	using Tests.MetaTemplate;
	using WallE.Base;
	using WallE.Clients;
	using WallE.Eve;
	using WallE.RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "By design, though could potentially use a rewrite per TODO, below")]
	public partial class FormTestBed : Form
	{
		#region Fields
		private int indent = 0;
		#endregion

		#region Constructors
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Unless I'm missing something, I think CA is just confused here.")]
		public FormTestBed() => this.InitializeComponent();
		#endregion

		#region Public Properties
		public Site AdminWiki { get; private set; }

		public Site Wiki { get; private set; }
		#endregion

		#region Public Static Methods
		public static void DebugResponseEventHandler(IWikiAbstractionLayer sender, ResponseEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine(e.Response, sender.ToString());
		}

		public static void DebugShowDelay(IMediaWikiClient sender, DelayEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			var aborted = string.Empty;

			if (e.Cancel)
			{
				aborted = "Aborted ";
			}

			Debug.WriteLine($"{aborted}Delay: {e.DelayTime} milliseconds. Reason: {e.Reason}", sender.ToString());
		}

		public static void DebugShowRequest(IWikiAbstractionLayer sender, RequestEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine(e.Request.ToString(), sender.ToString());
		}

		public static void DebugWarningEventHandler(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine($"Warning ({e.Warning.Code}): {e.Warning.Info}", sender.ToString());
		}

		public static void DumpTitles(IEnumerable<Title> titles)
		{
			var count = 0;
			foreach (var entry in titles)
			{
				Debug.WriteLine(entry.FullPageName);
				count++;
			}

			Debug.WriteLine(count);
		}
		#endregion

		#region Tests and Related
		public void AllMessagesTest()
		{
			var titles = new TitleCollection(this.Wiki);
			titles.AddMessages(Filter.Only);
			DumpTitles(titles);
		}

		public void Assert(bool condition, string message)
		{
			if (!condition)
			{
				this.AppendResults(message);
			}
		}

		public void BacklinksTests()
		{
			var titles = new TitleCollection(this.Wiki);
			titles.AddBacklinks("Oblivion:Oblivion", BacklinksTypes.Backlinks | BacklinksTypes.EmbeddedIn, true, Filter.Any, MediaWikiNamespaces.Template);
			this.CheckCollection(titles, "Backlinks");
			DumpTitles(titles);
		}

		public void BlocksTests()
		{
			var result = this.Wiki.GetBlocks(new[] { "RobinHood70", "HoodBot", "HotnBOThered", "Dagoth Ur" });
			foreach (var item in result)
			{
				var expiry = item.Expiry == DateTime.MaxValue ? "indefinite" : item.Expiry.ToString();
				Debug.WriteLine($"{item.User} blocked by {item.BlockedBy} on {item.StartTime}. Expires: {expiry}");
			}

			result = this.Wiki.GetBlocks(Filter.Only, Filter.Any, Filter.Exclude, Filter.Any);
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

		public void CategoryMembersTests()
		{
			var titles = new PageCollection(this.Wiki);
			titles.AddCategoryMembers("Books-Images", CategoryMemberTypes.File, true);
			this.CheckCollection(titles, "CategoryMembers");
			DumpTitles(titles);
		}

		public void CheckCollection<T>(IReadOnlyCollection<T> collection, string name)
		{
			if (collection == null)
			{
				this.AppendResults($"Collection {name} is null");
				return;
			}

			if (collection.Count == 0)
			{
				this.AppendResults($"Collection {name} has no members");
			}
		}

		public void CheckForNull(object check, string name)
		{
			if (check == null)
			{
				this.AppendResults($"{name} is null");
			}
		}

		public void CheckPagesResult(IReadOnlyList<PageItem> pages)
		{
			ThrowNull(pages, nameof(pages));
			this.CheckCollection(pages, "pages");
			if (pages != null)
			{
				if (pages.Count == 0)
				{
					this.AppendResults("No pages in output");
				}
				else
				{
					this.CheckCollection(pages[0].Revisions, "Revisions");
				}
			}
		}

		public void CategoryTests()
		{
			var titles = new TitleCollection(this.Wiki);
			titles.AddCategories("Arena-A", "Arena-J");
			DumpTitles(titles);
		}

		public void DeleteTests()
		{
			var page = new Page(this.AdminWiki, "Delete Test")
			{
				Text = "Test page to be deleted."
			};
			page.Save("Create test page", false);
			page.Delete("Delete test page");
		}

		public void DuplicateFilesTests()
		{
			const string duped = "File:ON-icon-ava-Defensive Scroll Bonus I.png";
			var pageCollection = new PageCollection(this.Wiki);
			pageCollection.AddDuplicateFiles(new TitleCollection(this.Wiki, duped));
			DumpTitles(pageCollection);

			var filePage = new FilePage(this.Wiki, duped);
			var result = filePage.FindDuplicateFiles();
			var files = new TitleCollection(this.Wiki, MediaWikiNamespaces.File, result);
			DumpTitles(files);
		}

		public void FileUsagesTests()
		{
			const string used = "File:EnwiktwatchlistCapture.PNG";
			var filePage = new FilePage(this.Wiki, used);
			var result = filePage.FileUsage();
			var files = TitleCollection.CopyFrom(result);
			DumpTitles(files);
		}

		public void MetaTemplateTests()
		{
			this.Wiki.DefaultLoadOptions = new PageLoadOptions(PageModules.Info | PageModules.Revisions | PageModules.Custom);
			var titles = new TitleCollection(this.Wiki, "Legends:Adoring Fan");
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

		public void MoveTests()
		{
			var page = new Page(this.AdminWiki, "Move Wrong Page Test")
			{
				Text = "Test page to be moved " + DateTime.UtcNow.ToString()
			};
			page.Save("Create test page", false);

			Debug.WriteLine(page.Move("Move Test", "Move test page", true));
		}

		public void NamespaceTests()
		{
			var nss = this.Wiki.Namespaces;
			this.Assert(nss["template"].Id == 10, "String indexing not working.");
			this.Assert(nss[0] == nss[MediaWikiNamespaces.Main] && nss[0] == nss[string.Empty], "Equivalent namespaces aren't.");
			this.Assert(nss[MediaWikiNamespaces.File] == "Image", "Namespace equals string failed.");
			this.Assert(nss[MediaWikiNamespaces.Template].Id == MediaWikiNamespaces.Template, "Namespace equals enum failed.");

			nss.AddToNames("Main", this.Wiki.Namespaces[MediaWikiNamespaces.Main]);
			this.Assert(nss["main"].Id == 0, "Main namespace does not appear to have been added.");
		}

		public void PageCollectionFromCategoriesTest()
		{
			var sourcePages = new TitleCollection(this.Wiki, "Main Page");
			var pageCollection = new PageCollection(this.Wiki);
			pageCollection.AddPageCategories(sourcePages);
			foreach (var page in pageCollection)
			{
				this.Assert(page.Namespace.Id == MediaWikiNamespaces.Category, "A page in the returned collection isn't a category.");
			}
		}

		public void PageCollectionFromQueryPage()
		{
			var pageCollection = new PageCollection(this.Wiki);
			pageCollection.AddQueryPage("Mostlinked");
			DumpTitles(pageCollection);
		}

		public void PagesCategoriesOnTests()
		{
			var pages = new PageCollection(this.Wiki) { LoadOptions = PageLoadOptions.None };
			var categoryTitles = new TitleCollection(this.Wiki, "API:Categories", "API:Purge");
			pages.AddPageCategories(categoryTitles, Filter.Any);
			DumpTitles(pages);
		}

		public void PageTests()
		{
			var pages = new PageCollection(this.Wiki);
			pages.AddTitles("MediaWiki:1movedto2");
			foreach (var page in pages)
			{
				Debug.WriteLine($"Invalid: {page.Invalid}; Missing: {page.Missing}; Text: {page.Text}");
			}

			this.Assert(Page.CheckExistence(this.Wiki, "Main Page"), "Main Page not detected as existing.");
			this.Assert(!Page.CheckExistence(this.Wiki, "This page does not exist"), "Non-existent page detected as existing.");
			this.Assert(new Title(this.Wiki, "Template:Test").IsSameTitle(new Page(this.Wiki, "Template:Test")), "Title and Page should be equal, but aren't.");
		}

		public void PageTypeTests()
		{
			var loadOptions = new PageLoadOptions(PageModules.All) { ImageRevisionCount = 5 };
			var pageCollection = new PageCollection(this.Wiki, loadOptions);
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

		public void ProtectTests()
		{
			var title = new Title(this.AdminWiki, "Create Protection Test");
			title.CreateProtect("Test create protection", ProtectionLevel.Full, DateTime.Now + new TimeSpan(0, 1, 0));
			title.CreateUnprotect("Test create unprotection");

			var page = new Page(this.AdminWiki, "Protection Test Page")
			{
				Text = "Protection test page: " + DateTime.UtcNow.ToString()
			};
			page.Save("Create test page", false);
			page.Protect("Test protection", ProtectionLevel.Full, ProtectionLevel.Semi, null);
			page.Unprotect("Test unprotection", true, true);
		}

		public void ProtectedTitlesTests()
		{
			var titles = new TitleCollection(this.Wiki);
			titles.AddProtectedTitles();
			this.CheckCollection(titles, "ProtectedTitles");
			DumpTitles(titles);
		}

		public void PurgeTests()
		{
			var titles = new TitleCollection(this.Wiki, "User:RobinHood70");
			var result = titles.Purge(PurgeMethod.Normal);
			DumpTitles(result);
		}

		public void RecentChangesTests()
		{
			var result = this.Wiki.GetRecentChanges();
			Debug.WriteLine(result.Count);
		}

		public void RedirectTargetTests()
		{
			var target = this.Wiki.GetRedirectTarget("#REDIRECT [[Template:Hello]]");
			this.Assert(target.FullPageName == "Template:Hello", "Incorrect template target.");

			target = this.Wiki.GetRedirectTarget("#WEITERLEITUNG [[Template:Hello|Stupid text]]][[Flower]]");
			this.Assert(target.FullPageName == "Template:Hello", "Incorrect template target.");

			target = this.Wiki.GetRedirectTarget(" #REDIRECT [[Hello world]]");
			this.Assert(target != null, "Incorrectly detected a malformed redirect.");
		}

		public void SearchTests()
		{
			var titles = new TitleCollection(this.Wiki);
			titles.AddSearchResults("aleph", WhatToSearch.Title, this.Wiki.Namespaces.RegularIds);
			DumpTitles(titles);
		}

		public void TemplateTransclusionTest()
		{
			var titleCollection = new TitleCollection(this.Wiki);
			titleCollection.AddTransclusions();
			DumpTitles(titleCollection);
		}

		public void TitleTests()
		{
			this.Assert(Title.Normalize("Hello\u200E\u200F\u202A\u202B\u202C\u202D\u202E_\xA0\u1680\u180E\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u2028\u2029\u202F\u205F\u3000World") == "Hello                    World", "Text was not fully stripped and/or replaced.");
			this.Assert(Title.PipeTrick("(Test)") == "(Test)", "PipeTrick failed for (Test).");
			this.Assert(Title.PipeTrick("Hello (Test)") == "Hello", "PipeTrick failed for Hello (Test).");
			this.Assert(Title.PipeTrick("Hello (Test), Goodbye") == "Hello", "PipeTrick failed for Hello (Test), Goodbye.");
			this.Assert(Title.PipeTrick("Hello, Goodbye (Test)") == "Hello, Goodbye", "PipeTrick failed for Hello, Goodbye (Test).");
			this.Assert(Title.NameFromParts(this.Wiki.Namespaces[10], "!", null) == "Template:!", "NameFromParts failed for Template:!");
			this.Assert(Title.NameFromParts(this.Wiki.Namespaces[0], "Main Page", "Test") == "Main Page#Test", "NameFromParts failed for Main Page#Test.");

			var title = new Title(this.Wiki, "Template:!");
			this.Assert(title.Namespace.Id == 10, "Namespace was incorrect for Template:!.");
			this.Assert(title.PageName == "!", "PageName was incorrect for Template:!.");
			this.Assert(title.SubjectPage.FullPageName == "Template:!", "SubjectPage was incorrect for Template:!.");
			this.Assert(title.TalkPage.FullPageName == "Template talk:!", "TalkPage was incorrect for Template:!.");
		}

		public void TitlesAllPagesTests()
		{
			var titles = new TitleCollection(this.Wiki);
			var sw = new Stopwatch();
			sw.Start();
			titles.AddNamespace(MediaWikiNamespaces.Template, Filter.Any, "A", "C");
			Debug.WriteLine("Count: " + titles.Count);
			titles.Clear();
			titles.AddNamespace(MediaWikiNamespaces.Template, Filter.Any, "A", "B");
			Debug.WriteLine("Count: " + titles.Count);
			titles.AddNamespace(MediaWikiNamespaces.Template, Filter.Any, "A", "C");
			Debug.WriteLine("Count: " + titles.Count);
			Debug.WriteLine("Time: " + sw.ElapsedMilliseconds);
			foreach (var title in titles)
			{
				Debug.WriteLine(title.PageName);
			}
		}

		public void UnwatchTests()
		{
			var titles = new TitleCollection(this.Wiki, "User:RobinHood70");
			var result = titles.Unwatch();
			DumpTitles(result);
		}

		public void UploadRandomImage(string destinationName)
		{
			if (this.Wiki.ServerName != "rob-centos")
			{
				throw new InvalidOperationException("You're uploading porn to a wiki that's not yours, dumbass!");
			}

			var rand = new Random();
			var files = Directory.GetFiles(@"C:\Users\rmorl\Pictures\Screen Saver Pics\", "*.jpg"); // Only select from jpgs so we don't have to worry about extension type.
			var fileName = files[rand.Next(files.Length)];
			this.Wiki.Upload(fileName, destinationName, "Test upload");
		}

		public void UserBlockTests()
		{
			var user = new User(this.AdminWiki, "Test User");
			user.Block("Because he's a bad person", BlockFlags.AutoBlock | BlockFlags.AllowUserTalk, "5 minutes", true);
			user.Unblock("Because he's a good person");
		}

		public void UserContributionsTests()
		{
			var user = new User(this.Wiki, "RobinHood70");
			var result = user.GetContributions();
			var titles = new HashSet<string>();
			foreach (var item in result)
			{
				titles.Add(item.Title.FullPageName);
			}

			Debug.WriteLine($"{result.Count} contributions on {titles.Count} pages");
		}

		public void UserEmailTests()
		{
			var user = new User(this.Wiki, "RobinHood70");
			var result = user.Email("This is a test e-mail.", true);
			Debug.WriteLine(result);
		}

		public void UserFullInfoTests()
		{
			var userLoad = new User(this.Wiki, "RobinHood70");
			userLoad.Load();
			Debug.WriteLine(string.Join(",", userLoad.Groups));
			Debug.WriteLine(userLoad.Gender.UpperFirst());

			var users = this.Wiki.GetUserInformation("RobinHood70", "Test User");
			foreach (var user in users)
			{
				Debug.Write('\n');
				Debug.WriteLine(user.Name);
				Debug.WriteLine(string.Join(",", user.Groups));
				Debug.WriteLine(user.Gender.UpperFirst());
			}
		}

		public void UserMessageTests()
		{
			var user = new User(this.Wiki, "RobinHood70");
			user.NewTalkPageMessage("Test Message", "Hi there!", "Create a test message.");
		}

		public void UsersTests()
		{
			Debug.WriteLine("Active Users: {0}", this.Wiki.GetUsers(true, false).Count);
			Debug.WriteLine("Sysops: {0}", this.Wiki.GetUsersInGroups(false, false, "sysop").Count);
			Debug.WriteLine("API High Limits: {0}", this.Wiki.GetUsersWithRights(false, false, "apihighlimits").Count);
			Debug.WriteLine("API High Limits with edits: {0}", this.Wiki.GetUsersWithRights(false, true, "apihighlimits").Count);
			Debug.WriteLine("API High Limits that are active: {0}", this.Wiki.GetUsersWithRights(true, false, "apihighlimits").Count);
		}

		public void UserWatchlistTests()
		{
			var user = new User(this.AdminWiki, "RobinHood70");
			var result = user.GetWatchlist("625ff6a84caa118b313fab35cbdf75f356cb93b8", this.Wiki.Namespaces.RegularIds);
			Debug.WriteLine($"RobinHood70 has {result.Count} pages in his watchlist.");
		}

		public void WatchTests()
		{
			var titles = new TitleCollection(this.Wiki, "User:RobinHood70");
			var result = titles.Watch();
			DumpTitles(result);
		}
		#endregion

		#region Private Static Methods
		private static string GetHmac(string message, string key)
		{
			var sb = new StringBuilder(64);
			var encoding = Encoding.UTF8;
			var keyBytes = encoding.GetBytes(key);
			var messageBytes = encoding.GetBytes(message);
			byte[] hash;
			using (var hmacsha1 = new HMACSHA1(keyBytes))
			{
				hash = hmacsha1.ComputeHash(messageBytes);
			}

			foreach (var b in hash)
			{
				sb.Append(b.ToString("X2"));
			}

			return sb.ToString().ToLowerInvariant();
		}
		#endregion

		#region Private Methods
		private void AppendResults(string message)
		{
			message = new string(' ', this.indent) + message + Environment.NewLine;
			if (this.textBoxResults.InvokeRequired)
			{
				this.textBoxResults.Invoke(new Action<string>(this.AppendResults), message);
			}
			else
			{
				this.textBoxResults.AppendText(message);
			}
		}

		private void ButtonClear_Click(object sender, EventArgs e) => this.textBoxResults.Clear();

		private void ButtonQuick_Click(object sender, EventArgs e)
		{
			this.ButtonQuick.Enabled = false;
			var wikiInfo = this.ComboBoxWiki.SelectedItem as WikiInfo;
			this.DoGlobalSetup(wikiInfo);

			this.CategoryMembersTests();

			this.DoGlobalTeardown(wikiInfo);
			this.ButtonQuick.Enabled = true;
		}

		private void DoGlobalSetup(WikiInfo wikiInfo)
		{
			IMediaWikiClient baseClient = new SimpleClient(wikiInfo.UserName, @"D:\Data\WallE\cookies.dat");
			var client = (wikiInfo.ReadInterval == 0 && wikiInfo.WriteInterval == 0)
				? baseClient
				: new ThrottledClient(baseClient, TimeSpan.FromMilliseconds(wikiInfo.ReadInterval), TimeSpan.FromMilliseconds(wikiInfo.WriteInterval));
			//// client.RequestingDelay += DebugShowDelay;

			var wal = new WikiAbstractionLayer(client, wikiInfo.Uri);
			wal.SendingRequest += DebugShowRequest;
			wal.WarningOccurred += DebugWarningEventHandler;
			wal.StopCheckMethods &= ~StopCheckMethods.Assert;
			//// wal.ResponseReceived += DebugResponseEventHandler;

			if (wikiInfo.Name.Contains("UESP"))
			{
				wal.ModuleFactory.RegisterProperty<VariablesInput>(PropVariables.CreateInstance);
				wal.ModuleFactory.RegisterGenerator<VariablesInput>(PropVariables.CreateInstance);
			}

			this.Wiki = new Site(wal);
			this.Wiki.WarningOccurred += Robby.Site.DebugWarningEventHandler;
			this.Wiki.Login(wikiInfo.UserName, wikiInfo.Password);

			if (wikiInfo.Name.Contains("UESP"))
			{
				this.Wiki.PageCreator = new MetaTemplateBuilder();
			}

			if (wikiInfo.AdminUserName != null)
			{
				var adminClient = new SimpleClient(null, @"D:\Data\WallE\cookiesAdmin.dat")
				{
					Name = "Admin",
				};
				wal = new WikiAbstractionLayer(adminClient, wikiInfo.Uri);
				wal.SendingRequest += DebugShowRequest;
				wal.WarningOccurred += DebugWarningEventHandler;
				wal.Assert = null;
				wal.StopCheckMethods &= ~StopCheckMethods.Assert;
				this.AdminWiki = new Site(wal);
				this.AdminWiki.WarningOccurred += Robby.Site.DebugWarningEventHandler;
				this.AdminWiki.Login(wikiInfo.AdminUserName, wikiInfo.AdminPassword);
				this.RunJobs(wikiInfo.SecretKey);
			}
		}

		private void DoGlobalTeardown(WikiInfo wikiInfo)
		{
			this.RunJobs(wikiInfo.SecretKey);
			this.Wiki = null;
			this.AdminWiki = null;
		}

		private void FormTestBed_Load(object sender, EventArgs e)
		{
			foreach (var line in File.ReadAllLines("WikiList.txt"))
			{
				this.ComboBoxWiki.Items.Add(new WikiInfo(line));
			}

			if (this.ComboBoxWiki.Items.Count > 0)
			{
				this.ComboBoxWiki.SelectedIndex = 0;
			}
		}

		private void RunJobs(string secretKey)
		{
			if (this.AdminWiki?.AbstractionLayer is WikiAbstractionLayer wal && secretKey.Length > 0)
			{
				var path = this.AdminWiki.GetArticlePath(string.Empty);
				var request = new Request(path, RequestType.Post, false);
				var message = request
					.Add("async", true)
					.Add("maxjobs", 1000)
					.Add("sigexpiry", (int)(DateTime.UtcNow.AddSeconds(5) - new DateTime(1970, 1, 1)).TotalSeconds)
					.Add("tasks", "jobs")
					.Add("title", "Special:RunJobs")
					.ToString();
				message = message.Substring(message.IndexOf('?') + 1);
				request.Add("signature", GetHmac(message, secretKey));
				wal.SendRequest(request);
			}
		}
		#endregion
	}
}
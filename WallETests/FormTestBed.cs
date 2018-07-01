namespace RobinHood70.WallE.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Net.Http;
	using System.Security.Cryptography;
	using System.Text;
	using System.Threading;
	using System.Windows.Forms;
	using RobinHood70.TestingCommon;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static WikiCommon.Globals;

	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "By design, though could potentially use a rewrite per TODO, below")]
	public partial class FormTestBed : Form
	{
		#region Fields
		private WikiAbstractionLayer adminWiki;
		private int indent = 0;
		private Uri indexPath;
		private Stopwatch sw = new Stopwatch();
		private int timePos;
		private WikiAbstractionLayer wiki;
		private WikiInfo wikiInfo;
		#endregion

		#region Constructors
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Unless I'm missing something, I think CA is just confused here.")]
		public FormTestBed() => this.InitializeComponent();
		#endregion

		#region Public Static Properties
		public static IEnumerable<IPropertyInput> DefaultPageProperties => new IPropertyInput[]
		{
			new InfoInput(),
			new RevisionsInput { Properties = RevisionsProperties.All },
		};

		public static IEnumerable<IPropertyInput> TestPageProperties => new IPropertyInput[]
		{
			new InfoInput { Properties = InfoProperties.All },
			new RevisionsInput() { Properties = RevisionsProperties.All },
			new CategoriesInput() { Properties = CategoriesProperties.All },
			new LanguageLinksInput() { Properties = LanguageLinksProperties.All },
			new LinksInput(),
			new ImagesInput(),
			new InterwikiLinksInput() { Properties = InterwikiLinksProperties.Url },
			new TemplatesInput(),
			new ExternalLinksInput(),
			new PagePropertiesInput(),
			new ContributorsInput(),
			new DuplicateFilesInput(),
			new ImageInfoInput() { Properties = ImageProperties.All },
			new CategoryInfoInput(),
		};
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
			Debug.WriteLine("{0} - {1}", sender.UserName, e.Request);
		}

		public static void DebugWarningEventHandler(IWikiAbstractionLayer sender, WarningEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine($"Warning ({e.Warning.Code}): {e.Warning.Info}", sender.ToString());
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
		/*
		private void BlockChanger(object sender, EventArgs e)
		{
		const int NumYears = 1;

		this.ButtonQuick.Enabled = false;
		var wiki = this.WikiInfo;
		this.DoGlobalSetup(wiki.Uri, wiki.UserName, wiki.Password, true);
		Global.wiki.ClearHasMessage();

		try
		{
		var blocksInput = new BlocksInput();
		blocksInput.Properties = BlocksProperties.Expiry | BlocksProperties.Flags | BlocksProperties.Reason | BlocksProperties.Timestamp | BlocksProperties.User;
		blocksInput.ShowAccount = false;
		blocksInput.ShowIP = true;
		blocksInput.ShowRange = false;
		blocksInput.ShowTemp = false;

		var comparer = CultureInfo.InvariantCulture.CompareInfo;

		var blocks = Global.wiki.BlocksLoad(blocksInput);
		foreach (var block in blocks)
		{
		if (comparer.IndexOf(block.Reason, "proxy", CompareOptions.IgnoreCase) >= 0 && comparer.IndexOf(block.Reason, "tor", CompareOptions.IgnoreCase) == -1)
		{
		continue;
		}

		if (block.Timestamp.Value <= DateTime.Now.AddYears(-NumYears))
		{
		var unblock = new UserUnblockInput(block.User);
		unblock.Reason = "Remove infinite IP block";
		Global.wiki.UserUnblock(unblock);
		}
		else
		{
		var newBlock = new UserBlockInput(block.User);
		newBlock.AllowUserTalk = block.AllowUserTalk;
		newBlock.AnonymousOnly = block.AnonymousOnly;
		newBlock.AutoBlock = block.AutoBlock;
		newBlock.Expiry = block.Timestamp.Value.AddYears(NumYears);
		newBlock.NoCreate = block.NoCreate;
		newBlock.NoEmail = false;
		newBlock.Reason = "Re-block with finite block length";
		newBlock.Reblock = true;
		newBlock.User = block.User;

		Global.wiki.UserBlock(newBlock);
		}

		FormTestBed.CheckTalkPage();
		}
		}
		catch (WikiException ex)
		{
		MessageBox.Show(ex.ErrorInfo, ex.ErrorCode);
		}

		Global.wiki.SiteLogout();
		this.ButtonQuick.Enabled = true;
		}

		private static void CheckTalkPage()
		{
		var userInfo = Global.wiki.UserGetInfo(new UserInfoInput(UserInfoProperties.HasMsg));
		}
		*/

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

		private void Assert(bool condition, string message)
		{
			if (!condition)
			{
				this.AppendResults(message);
			}
		}

		private void AssertBeforeVersion(int version, bool condition, string message)
		{
			if (this.wiki.SiteVersion < version)
			{
				this.Assert(condition, message);
			}
		}

		private void AssertBetweenVersions(int minVersion, int maxVersion, bool condition, string message)
		{
			if (this.wiki.SiteVersion >= minVersion && this.wiki.SiteVersion <= maxVersion)
			{
				this.Assert(condition, message);
			}
		}

		private void AssertVersion(int version, bool condition, string message)
		{
			if (this.wiki.SiteVersion >= version)
			{
				this.Assert(condition, message);
			}
		}

		private void ButtonClear_Click(object sender, EventArgs e) => this.textBoxResults.Clear();

		private void ButtonQuick_Click(object sender, EventArgs e)
		{
			// const string PageName = "Albert Einstein";
			this.ButtonQuick.Enabled = false;
			this.DoGlobalSetup();
			this.Login();

			this.DoGlobalTeardown();
			this.ButtonQuick.Enabled = true;
		}

		private void ButtonRunAll_Click(object sender, EventArgs e)
		{
			this.ButtonRunAll.Enabled = false;

			this.DoGlobalSetup();
			if (this.wiki.Uri.Host == "rob-centos")
			{
				this.wiki.Logout(); // Since we're logged in by default now, specifically log out for the tests.
				this.LoginTests();
			}

			this.Login();

			this.TokenTests();
			this.SiteInfoTests();
			this.AllMessagesTests();
			this.UserInfoTests();
			this.BacklinksTests();
			this.CategoriesTests();
			this.AllFileUsagesTests();
			this.AllLinksTest();
			this.AllRedirectsTest();
			this.AllTransclusionsTest();
			this.AllImagesTests();
			this.ParameterInfoTests();
			this.ParseTests();
			this.ResetPasswordTests();
			this.RsdTests();
			this.EmailUserTests();
			this.ImageRotateTests();
			this.ImportTests();
			this.ManageTagsTests();
			this.MergeHistoryTests();
			this.MoveTests();
			this.OptionsTests();
			this.PatrolTests();
			this.ProtectTests();
			this.PurgeTests();
			this.RevisionDeleteTests();
			this.RollbackTests();
			this.SetNotificationTimestampTests();
			this.TagsTests();
			this.UndeleteTests();
			this.UserRightsTests();
			this.WatchTests();

			this.DoGlobalTeardown();
			this.ButtonRunAll.Enabled = true;
		}

		private void CheckCollection<T>(IReadOnlyCollection<T> collection, string name)
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

		private void CheckForNull(object check, string name)
		{
			if (check == null)
			{
				this.AppendResults($"{name} is null");
			}
		}

		private void CheckPagesResult<TPageItem>(PageSetResult<TPageItem> pages)
			where TPageItem : PageItem
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
					this.CheckCollection(pages.First().Revisions, "Revisions");
				}
			}
		}

		private void ComboBoxWiki_SelectedIndexChanged(object sender, EventArgs e) => this.wikiInfo = this.ComboBoxWiki.SelectedItem as WikiInfo;

		private void DoGlobalSetup()
		{
			IMediaWikiClient client;
			var baseClient = new SimpleClient(null, @"D:\Data\WallE\cookies.dat")
			{
				Name = "Normal",
			};
			if (this.wikiInfo.ReadInterval == 0 && this.wikiInfo.WriteInterval == 0)
			{
				client = baseClient;
			}
			else
			{
				client = new ThrottledClient(baseClient, TimeSpan.FromMilliseconds(this.wikiInfo.ReadInterval), TimeSpan.FromMilliseconds(this.wikiInfo.WriteInterval));
			}

			//// client.ResponseReceived += DebugResponseEventHandler;
			//// client.RequestingDelay += DebugShowDelay;

			var wal = new WikiAbstractionLayer(client, this.wikiInfo.Uri);
			wal.SendingRequest += DebugShowRequest;
			wal.WarningOccurred += DebugWarningEventHandler;
			wal.Assert = null;
			wal.StopCheckMethods = wal.StopCheckMethods & ~StopCheckMethods.Assert;
			try
			{
				var result = wal.IsEnabled();
				if (!result)
				{
					throw new InvalidOperationException("API not enabled");
				}
			}
			catch (HttpRequestException)
			{
				// Wiki is down.
				throw;
			}

			this.wiki = wal;

			if (this.wikiInfo.AdminUserName != null)
			{
				var adminClient = new SimpleClient(null, @"D:\Data\WallE\cookiesAdmin.dat")
				{
					Name = "Admin",
				};
				wal = new WikiAbstractionLayer(adminClient, this.wikiInfo.Uri);
				wal.SendingRequest += DebugShowRequest;
				wal.WarningOccurred += DebugWarningEventHandler;
				wal.Assert = null;
				wal.StopCheckMethods = wal.StopCheckMethods & ~StopCheckMethods.Assert;
				this.adminWiki = wal;
			}
		}

		private void DoGlobalTeardown()
		{
			this.RunJobs();
			this.wiki = null;
			this.adminWiki = null;
		}

		private DeleteResult Delete(string title) => this.Delete(title, "Remove previous test");

		private DeleteResult Delete(string title, string reason)
		{
			try
			{
				var delete = new DeleteInput(title) { Reason = reason };
				return this.adminWiki.Delete(delete);
			}
			catch (WikiException)
			{
			}

			return null;
		}

		private EditResult Edit(string title, string content, string summary) => this.wiki.Edit(new EditInput(title, content) { Summary = summary });

		private EditResult EditAdmin(string title, string content, string summary) => this.adminWiki.Edit(new EditInput(title, content) { Summary = summary });

		private void FormTestBed_Load(object sender, EventArgs e)
		{
			var allWikiInfo = WikiInfo.LoadFile();
			foreach (WikiInfo item in allWikiInfo)
			{
				this.ComboBoxWiki.Items.Add(item);
			}

			if (this.ComboBoxWiki.Items.Count > 0)
			{
				this.ComboBoxWiki.SelectedIndex = 0;
			}
		}

		private void Insert(string message)
		{
			this.textBoxResults.Text = this.textBoxResults.Text.Insert(this.timePos, message);
			this.textBoxResults.SelectionStart = this.textBoxResults.Text.Length;
			this.textBoxResults.ScrollToCaret();
		}

		private void Login()
		{
			var loginInput = new LoginInput(this.wikiInfo.UserName, this.wikiInfo.Password);
			var result = this.wiki.Login(loginInput);
			if (result.UserId == 0)
			{
				throw new InvalidOperationException("User login failed: " + result.Reason);
			}

			if (this.wikiInfo.AdminUserName != null)
			{
				loginInput = new LoginInput(this.wikiInfo.AdminUserName, this.wikiInfo.AdminPassword);
				result = this.adminWiki.Login(loginInput);
				if (result.UserId == 0)
				{
					throw new InvalidOperationException("Admin login failed!");
				}

				this.indexPath = this.adminWiki.GetArticlePath(string.Empty);

				this.RunJobs();
			}
		}

		private void RunJobs()
		{
			if (this.wikiInfo.SecretKey.Length > 0)
			{
				var request = new Request(this.indexPath, RequestType.Post, false);
				var message = request
					.Add("async", true)
					.Add("maxjobs", 1000)
					.Add("sigexpiry", (int)(DateTime.UtcNow.AddSeconds(5) - new DateTime(1970, 1, 1)).TotalSeconds)
					.Add("tasks", "jobs")
					.Add("title", "Special:RunJobs")
					.ToString();
				message = message.Substring(message.IndexOf('?') + 1);
				request.Add("signature", GetHmac(message, this.wikiInfo.SecretKey));
				this.adminWiki.SendRequest(request);
			}
		}

		private void ShowStopwatch()
		{
			this.indent -= 2;
			this.Insert(Invariant($": {this.sw.ElapsedMilliseconds} ms"));
			this.sw.Stop();
		}

		private void StartStopwatch(string testName)
		{
			this.AppendResults(testName);
			this.timePos = this.textBoxResults.Text.Length - 2;
			this.indent += 2;
			this.sw.Restart();
		}

		private void UploadRandomImage(string destinationName)
		{
			if (this.wiki.Uri.Host != "rob-centos")
			{
				throw new InvalidOperationException("You're uploading porn to a wiki that's not yours, dumbass!");
			}

			var rand = new Random();
			var files = Directory.GetFiles(@"C:\Users\rmorl\Pictures\Screen Saver Pics\", "*.jpg"); // Only select from jpgs so we don't have to worry about extension type.
			var fileName = files[rand.Next(files.Length)];

			using (var upload = new FileStream(fileName, FileMode.Open))
			{
				this.wiki.Upload(new UploadInput(destinationName, upload)
				{
					IgnoreWarnings = true,
					Comment = "My comment",
					Text = "==License==\nYup, this is licensed.\n\n[[Category:Test Images]]",
				});
			}
		}
		#endregion

		#region Tests
		private void AllImagesTests()
		{
			this.StartStopwatch("AllImages");
			var input = new AllImagesInput();
			var result = this.wiki.AllImages(input);
			var pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);
			this.ShowStopwatch();
		}

		private void AllMessagesTests()
		{
			const string Message = "about";

			this.StartStopwatch("AllMessages");
			var input = new AllMessagesInput() { Messages = new List<string>() { Message }, LanguageCode = "bpy", IncludeLocal = true };
			var result = this.wiki.AllMessages(input);
			var resultFirst = result[0];
			if (resultFirst.Flags.HasFlag(MessageFlags.Missing))
			{
				this.AppendResults($"Failed to get '{Message}' message");
			}

			if (resultFirst.Content != "বারে")
			{
				this.AppendResults("Text mismatch");
			}

			input = new AllMessagesInput() { MessageFrom = "about", MessageTo = "aboutsite", LanguageCode = "en", IncludeLocal = true };
			result = this.wiki.AllMessages(input);
			if (result.Count == 0)
			{
				this.AppendResults("message range had no messages");
			}

			this.ShowStopwatch();
		}

		private void AllFileUsagesTests()
		{
			this.StartStopwatch("AllFileUsages");
			var input = new AllFileUsagesInput();
			var result = this.wiki.AllFileUsages(input);
			var pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result != null)
			{
				if (result.Count > 0)
				{
					this.CheckForNull(result[0].Title, "Title");
				}

				this.CheckPagesResult(pages);
			}

			this.CheckPagesResult(pages);
			this.ShowStopwatch();
		}

		private void AllLinksTest()
		{
			this.StartStopwatch("AllLinks");
			var input = new AllLinksInput();
			var result = this.wiki.AllLinks(input);
			var pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "output");
			if (result != null)
			{
				if (result.Count > 0)
				{
					this.CheckForNull(result[0].Title, "Title");
				}

				this.CheckPagesResult(pages);
			}

			this.CheckPagesResult(pages);
			this.ShowStopwatch();
		}

		private void AllRedirectsTest()
		{
			this.StartStopwatch("AllRedirects");
			var input = new AllRedirectsInput();
			var result = this.wiki.AllRedirects(input);
			var pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result != null)
			{
				if (result.Count > 0)
				{
					this.CheckForNull(result[0].Title, "Title");
				}

				this.CheckPagesResult(pages);
			}

			this.CheckPagesResult(pages);
			this.ShowStopwatch();
		}

		private void AllTransclusionsTest()
		{
			this.StartStopwatch("AllTransclusions");
			var input = new AllTransclusionsInput() { Namespace = 0 };
			var result = this.wiki.AllTransclusions(input);
			var pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);
			this.ShowStopwatch();
		}

		private void BacklinksTests()
		{
			this.Edit("Test Page 1", "{{File:Test Image 1.jpg}}[[:File:Test Image 1.jpg]][[File:Test Image 1.jpg|thumb|Test image]]\n{{:Test Page 2}}\n\n[[Category:Test Pages]]", "Create/modify test page");
			this.Edit("Test Page 2", "[[Test Page 1]]\n\n[[Category:Test Pages]]", "Create/modify test page");
			this.Edit("Category:Test Images", "This is the category for test images.", "Create category");
			this.Edit("Category:Test Pages", "This is the category for test pages.", "Create category");
			this.Edit("Test Redirect 1", "#REDIRECT [[Test Page 1]]", "Create redirect");
			this.UploadRandomImage("Test Image 1.jpg");
			this.StartStopwatch("Backlinks");
			var input = new BacklinksInput("Test Page 1", BacklinksTypes.Backlinks) { FilterRedirects = Filter.Any };
			var result = this.wiki.Backlinks(input);
			var pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);

			input = new BacklinksInput("File:Test Image 1.jpg", BacklinksTypes.EmbeddedIn) { FilterRedirects = Filter.Any };
			result = this.wiki.Backlinks(input);
			pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);

			input = new BacklinksInput("File:Test Image 1.jpg", BacklinksTypes.ImageUsage) { FilterRedirects = Filter.Any };
			result = this.wiki.Backlinks(input);
			pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);

			input = new BacklinksInput("File:Test Image 1.jpg", BacklinksTypes.All) { FilterRedirects = Filter.Any };
			result = this.wiki.Backlinks(input);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.ShowStopwatch();
		}

		private void CategoriesTests()
		{
			this.StartStopwatch("Categories");
			var input = new AllCategoriesInput() { From = "Test", To = "Tesz" };
			var result = this.wiki.AllCategories(input);
			var pages = this.wiki.LoadPages(new DefaultPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);
			this.ShowStopwatch();
		}

		private void EmailUserTests()
		{
			this.StartStopwatch("Email");
			var input = new EmailUserInput("RobinHood70", "This is a test.") { Subject = "Test Subject", CCMe = true };
			try
			{
				var result = this.wiki.EmailUser(input);
				this.Assert(result.Result == "Success", "E-mail user failed.");
			}
			catch (WikiException ex)
			{
				this.Assert(true, ex.Message);
			}

			this.ShowStopwatch();
		}

		private void ImageRotateTests()
		{
			const string imageName = "Test Image 1.jpg";
			this.UploadRandomImage(imageName);

			this.StartStopwatch("ImageRotate");
			var input = new ImageRotateInput(new[] { imageName }, 90);
			var result = this.wiki.ImageRotate(input);
			this.Assert(result.Count == 1, "Incorrect number of results.");
			this.ShowStopwatch();
		}

		private void ImportTests()
		{
			this.Delete("IngameEnding.as", "Remove previous test.");

			this.StartStopwatch("Import");
			var input = new ImportInput() { Xml = File.ReadAllText(@"D:\Data\WallE\Wiki26-20161214202753.xml"), FullHistory = true, Summary = "Test Import" };
			var result = this.adminWiki.Import(input);
			this.Assert(result.Count > 0, "No results after import.");
			this.Assert(result[0].Invalid == false, "Import result was invalid.");
			this.Assert(result[0].Revisions > 0, "Import revision count is 0.");
			this.ShowStopwatch();
		}

		private void LoginTests()
		{
			this.StartStopwatch("Login");
			var input = new LoginInput(this.wikiInfo.UserName, "Bad password");
			var result = this.wiki.Login(input);
			if (result.Result != "WrongPass" && result.Result != "Failed")
			{
				this.AppendResults($"Login did not detect an incorrect password. User: {result.User}");
			}

			input = new LoginInput("Nobody", "this will fail");
			result = this.wiki.Login(input);
			if (result.Result != "NotExists" && result.Result != "Failed")
			{
				this.AppendResults($"Login did not detect a non-existent user. User: {result.User}");
			}

			this.ShowStopwatch();
		}

		private void ManageTagsTests()
		{
			const string tagName = "Test tickles";
			ManageTagsInput input;
			ManageTagsResult result;
			try
			{
				input = new ManageTagsInput(TagOperation.Delete, tagName) { Reason = "Delete test tag if present", IgnoreWarnings = true };
				result = this.adminWiki.ManageTags(input);
			}
			catch (WikiException)
			{
			}

			this.StartStopwatch("ManageTags");
			try
			{
				input = new ManageTagsInput(TagOperation.Create, tagName) { Reason = "Test tag" };
				result = this.adminWiki.ManageTags(input);
				this.Assert(result.Success, "Manage Tags was not successful.");
			}
			catch (WikiException ex)
			{
				this.Assert(true, "Create tag threw an exception: " + ex.Info);
			}

			try
			{
				input = new ManageTagsInput(TagOperation.Delete, tagName);
				result = this.adminWiki.ManageTags(input);
			}
			catch (WikiException ex)
			{
				this.Assert(true, "Delete tag threw an exception: " + ex.Info);
			}

			this.ShowStopwatch();
		}

		private void MergeHistoryTests()
		{
			const string testPage1 = "Test Merge 1";
			const string testPage2 = "Test Merge 2";
			this.Delete(testPage1);
			this.Delete(testPage2);
			this.Edit(testPage1, Invariant($"This is test edit #1.{DateTime.UtcNow}"), "Test edit");
			Thread.Sleep(2000);
			this.Edit(testPage2, Invariant($"This is test edit #2.{DateTime.UtcNow}"), "Test edit");

			this.StartStopwatch("Merge");
			var input = new MergeHistoryInput(testPage1, testPage2)
			{
				Reason = "Testing",
			};
			this.adminWiki.MergeHistory(input);
			var revisionsInput = new RevisionsInput() { MaxItems = 3 };
			var pageSetInput = new DefaultPageSetInput(new string[] { testPage2 });
			var pageResult = this.wiki.LoadPages(pageSetInput, new IPropertyInput[] { revisionsInput });
			this.Assert(pageResult.Count == 1, "Incorrect number of pages loaded.");
			this.Assert(pageResult.First().Revisions.Count == 2, "Incorrect number of revisions loaded.");
			this.ShowStopwatch();
		}

		private void MoveTests()
		{
			const string pageNameWrong = "Test Mvoe";
			const string pageNameRight = "Test Move";
			this.Delete("User:" + pageNameRight);
			this.Delete("User talk:" + pageNameRight);
			this.Delete("User:" + pageNameRight + "/Subpage");
			this.Edit("User talk: " + pageNameRight + "/Subpage", "This page is deliberately in the way.", "Test edit");
			this.Edit("User:" + pageNameWrong, Invariant($"This is a test edit.{DateTime.UtcNow}"), "Test edit");
			this.Edit("User talk:" + pageNameWrong, Invariant($"This is a test comment.{DateTime.UtcNow}"), "Test edit");
			this.Edit("User:" + pageNameWrong + "/Subpage", Invariant($"This is a test sub-edit.{DateTime.UtcNow}"), "Test edit");
			this.Edit("User talk:" + pageNameWrong + "/Subpage", Invariant($"This is a test sub-comment.{DateTime.UtcNow}"), "Test edit");

			this.StartStopwatch("Move");
			var input = new MoveInput("User:" + pageNameWrong, "User:" + pageNameRight)
			{
				MoveSubpages = true,
				MoveTalk = true,
				NoRedirect = true,
				Reason = "Test move",
			};

			var result = this.wiki.Move(input);
			if (result.Count == 4)
			{
				this.Assert(result[0].Error == null, "Move main page threw an error.");
				this.Assert(result[1].Error == null, "Move talk page threw an error.");
				this.Assert(result[2].Error == null, "Move subpage page threw an error.");
				this.Assert(result[3].Error != null, "Move over existing talk subpage page did not throw the expected error.");
			}
			else
			{
				this.Assert(true, "Wrong number of results.");
			}

			this.ShowStopwatch();
		}

		private void OptionsTests()
		{
			this.StartStopwatch("Options");
			var input = new OptionsInput();
			var change = new Dictionary<string, string>
{
{ "ccmeonemails", "false" },
};
			input.Change = change;
			this.wiki.Options(input);

			change["ccmeonemails"] = "true";
			this.wiki.Options(input);
			this.ShowStopwatch();
		}

		private void ParameterInfoTests()
		{
			this.StartStopwatch("ParameterInfo");
			var input = new ParameterInfoInput(new string[] { "block", "clientlogin", "expandtemplates", "main", "query", "query+deletedrevs" }) { HelpFormat = HelpFormat.Raw };
			var result = this.wiki.ParameterInfo(input);
			var module = result["block"];
			this.AssertVersion(127, module.Parameters["user"].Type == "user", "'User' parameter for 'block' module is not of 'user' type.");
			this.AssertBeforeVersion(127, module.Parameters["user"].Type == "string", "'User' parameter for 'block' module is not of 'string' type.");
			this.AssertVersion(124, module.Parameters["token"].TokenType == "csrf", "Invalid token type for 'block' module.");

			if (result.TryGetValue("clientlogin", out module))
			{
				this.Assert(module.DynamicParameters.RawMessages.Count == 1, "DynamicParameters failed to load correctly for 'clientlogin' module.");
			}

			module = result["expandtemplates"];
			this.AssertBetweenVersions(117, 126, module.Examples.Count == 1, "Incorrect number of examples for 'expandtemplates' module.");
			this.AssertBetweenVersions(127, 127, module.Examples.Count == 2, "Incorrect number of examples for 'expandtemplates' module.");
			this.AssertVersion(128, module.Examples.Count == 1, "Incorrect number of examples for 'expandtemplates' module.");
			this.AssertVersion(125, module.Parameters["prop"].HighLimit == 500, "Incorrect HighLimit for 'expandtemplates' module.");
			this.AssertVersion(125, module.Parameters["prop"].Type == "valuelist" && module.Parameters["prop"].TypeValues[0] == "wikitext", "Incorrect Type/TypeValues for 'messageformat' parameter of 'expandtemplates' module.");
			this.AssertVersion(125, module.Parameters["prop"].Description.RawMessages[1].ForValue == "wikitext", "Incorrect ForValue for 'prop' parameter of 'exandtemplate's module.");

			module = result["main"];
			this.Assert(module.Description.Text != null || module.Description.RawMessages.Count > 0, "Main module did not load properly.");
			this.AssertVersion(124, module.Parameters["format"]?.Submodules["xmlfm"] == "xmlfm", "Incorrect submodules value for main module.");

			module = result["query"];
			this.AssertVersion(121, module.HelpUrls.Count == 4, "Incorrect number of HelpUrls for help module.");
			this.AssertVersion(125, module.Parameters["generator"].SubmoduleParameterPrefix == "g", "SubmoduleParameterPrefix incorrect for 'query' module.");

			module = result["deletedrevs"];
			this.AssertVersion(125, module.Flags == (ModuleFlags.Deprecated | ModuleFlags.ReadRights), "Incorrect flags for 'deletedrevs' module.");
			this.AssertVersion(125, module.Parameters["start"].Type == "timestamp" && module.Parameters["start"].Information[0].Text.RawMessages[0].Parameters.Count == 2, "Num value appears to have been processed in that whole nested mess in 'deletedrevs' module.");
			this.ShowStopwatch();
		}

		private void ParseTests()
		{
			this.StartStopwatch("Parse");
			var input = ParseInput.FromText("{{Test}} Hello {{subst:Test}} [[Category:Test]] [[File:Test.jpg|64px]] [[Main Page]]");
			input.ContentModel = "wikitext";
			input.Properties = ParseProperties.All;
			var result = this.wiki.Parse(input);
			this.CheckCollection(result.Categories, nameof(result.Categories));
			this.CheckForNull(result.CategoriesHtml, nameof(result.CategoriesHtml));
			this.CheckForNull(result.DisplayTitle, nameof(result.DisplayTitle));
			this.CheckForNull(result.HeadHtml, nameof(result.HeadHtml));
			this.CheckCollection(result.Images, nameof(result.Images));
			this.CheckCollection(result.LimitReportData, nameof(result.LimitReportData));
			this.CheckForNull(result.LimitReportHtml, nameof(result.LimitReportHtml));
			this.CheckCollection(result.Links, nameof(result.Links));
			this.CheckForNull(result.ParseTree, nameof(result.ParseTree));
			this.CheckForNull(result.Templates, nameof(result.Templates));
			this.CheckForNull(result.Text, nameof(result.Text));
			this.CheckForNull(result.Title, nameof(result.Title));
			this.CheckForNull(result.WikiText, nameof(result.WikiText));
			this.ShowStopwatch();
		}

		private void PatrolTests()
		{
			var editResult = this.Edit("Test Patrol 1", Invariant($"Test page: {DateTime.UtcNow}"), "Test edit");
			var recentChangesResult = this.wiki.RecentChanges(new RecentChangesInput() { MaxItems = 1, Properties = RecentChangesProperties.Ids, User = this.wiki.UserName });
			var rcid = recentChangesResult[0].Id;

			this.StartStopwatch("Patrol");
			var input = this.wiki.SiteVersion < 122 ? new PatrolInput(rcid) : PatrolInput.FromRevisionId(editResult.NewRevisionId);
			var result = this.adminWiki.Patrol(input);
			this.Assert(result.RecentChangesId == rcid, "Returned rcid didn't match expected rcid.");
			this.ShowStopwatch();
		}

		private void ProtectTests()
		{
			this.Edit("Test Protect 1", Invariant($"Protected page: {DateTime.UtcNow}"), "Test edit");
			this.StartStopwatch("Protect");
			var input = new ProtectInput("Test Protect 1") { Reason = "Test Protect" };
			var protections = new List<ProtectInputItem>
			{
				new ProtectInputItem("edit", "sysop"),
				new ProtectInputItem("move", "sysop"),
			};
			input.Protections = protections;
			var result = this.adminWiki.Protect(input);
			this.Assert(result.Title == input.Title, "Title changed after adding protection.");
			this.Assert(result.Reason == input.Reason, "Reason changed after adding protection.");
			this.Assert(result.Protections.Count == 2, "Incorrect number of protections.");

			protections.Clear();
			protections.Add(new ProtectInputItem("edit", "all"));
			protections.Add(new ProtectInputItem("move", "all"));
			result = this.adminWiki.Protect(input);
			this.ShowStopwatch();
		}

		private void PurgeTests()
		{
			this.StartStopwatch("Purge");
			var input = new PurgeInput(new[] { "Main Page" });
			var result = this.wiki.Purge(input);
			this.Assert(result.Count == 1, "Purge returned the wrong number of results.");
			this.ShowStopwatch();
		}

		private void RevisionDeleteTests()
		{
			this.Edit("Test RevisionDelete", "Test deletable: " + DateTime.UtcNow.ToStringInvariant(), "This should get revdel'd");
			var editResult = this.Edit("Test RevisionDelete", "Test keepable: " + DateTime.UtcNow.ToStringInvariant(), "This should stay");

			this.StartStopwatch("RevisionDelete");
			var input = new RevisionDeleteInput(RevisionDeleteType.Revision, new long[] { editResult.OldRevisionId }) { Hide = RevisionDeleteProperties.All, Reason = "Because" };
			var result = this.adminWiki.RevisionDelete(input);

			this.Assert(result.Status == "Success", "Hide revisions was not successful.");
			this.Assert(result[0].Id == editResult.OldRevisionId, "Ids didn't match.");
			this.ShowStopwatch();
		}

		private void RollbackTests()
		{
			this.EditAdmin("Test Rollback", "Legitimate edit", "I am a good person");
			this.Edit("Test Rollback", "Vandalism edit 1", "I am vandalizing ur wiki");
			this.Edit("Test Rollback", "Vandalism edit 2", "I am vandalizing ur wiki");
			this.Edit("Test Rollback", "Vandalism edit 3", "I am vandalizing ur wiki");

			this.StartStopwatch("Rollback");
			var input = new RollbackInput("Test Rollback", this.wiki.UserName) { Summary = "Rollback test vandalism" };
			var result = this.adminWiki.Rollback(input);

			this.Assert(result.RevisionId > result.OldRevisionId, "RevisionId should be greather than OldRevisionId");
			this.Assert(result.OldRevisionId > result.LastRevisionId, "OldRevisionId should be greather than LastRevisionId");

			try
			{
				this.adminWiki.Rollback(input);
				this.Assert(true, "Double-rollback failed to throw an error.");
			}
			catch (WikiException)
			{
			}

			this.ShowStopwatch();
		}

		private void RsdTests()
		{
			this.StartStopwatch("Rsd");
			var result = this.wiki.Rsd();
			this.Assert(result.StartsWith("<?xml", StringComparison.Ordinal), "Rsd result was not XML.");
			this.ShowStopwatch();
		}

		private void ResetPasswordTests()
		{
			this.StartStopwatch("ResetPassword");
			var input = ResetPasswordInput.FromEmail("robinhood70@live.ca");
			input.Capture = true;
			var result = this.adminWiki.ResetPassword(input);
			this.CheckForNull(result.Status, nameof(result.Status));
			this.CheckCollection(result.Passwords, nameof(result.Passwords));
			this.ShowStopwatch();
		}

		private void SetNotificationTimestampTests()
		{
			this.StartStopwatch("SetNotificationTimestamp");

			// Full watchlist version
			var input = new SetNotificationTimestampInput() { Timestamp = DateTime.Now - TimeSpan.FromHours(1) };
			var result = this.wiki.SetNotificationTimestamp(input);
			if (result.Count == 1)
			{
				this.Assert(Math.Abs((result.First().NotificationTimestamp.Value - input.Timestamp.Value).TotalSeconds) <= 1, "Timestamps didn't match");
			}
			else
			{
				this.Assert(true, "Incorrect number of pages returned");
			}

			// PageSet version
			this.Edit("Test Page 1", "Page 1", "SetNotificationTimestamp test page");
			this.Edit("Test Page 2", "[[Test Page 1]]", "SetNotificationTimestamp test page");
			input = new SetNotificationTimestampInput(new string[] { "Test Page 1", "Test Page 2" });
			result = this.wiki.SetNotificationTimestamp(input);
			this.Assert(result.Count == 2, "Incorrect number of pages returned");
			this.ShowStopwatch();
		}

		private void SiteInfoTests()
		{
			this.StartStopwatch("SiteInfo");
			var input = new SiteInfoInput { Properties = SiteInfoProperties.All };
			var result = this.wiki.SiteInfo(input);
			this.CheckForNull(result.BasePage, "BasePage");
			this.CheckForNull(result.MainPage, "MainPage");
			this.CheckForNull(result.SiteName, "SiteName");
			this.CheckForNull(result.Generator, "Generator");
			this.CheckCollection(result.LagInfo, "LagInfo");
			this.CheckCollection(result.MagicWords, "MagicWords");
			this.CheckCollection(result.NamespaceAliases, "NamespaceAliases");
			this.CheckCollection(result.Namespaces, "Namespaces");
			this.ShowStopwatch();
		}

		private void TagsTests()
		{
			const string tagName = "Test tickles";
			try
			{
				this.adminWiki.ManageTags(new ManageTagsInput(TagOperation.Create, tagName) { Reason = "Test tagging" });
			}
			catch (WikiException)
			{
			}

			var editInput = new EditInput("Test Tag 1", "Testing tagging: " + DateTime.UtcNow.ToString()) { Summary = "Test" };
			var editResult = this.wiki.Edit(editInput);

			this.StartStopwatch("Tags");
			var input = new TagInput() { RevisionIds = new[] { editResult.NewRevisionId, 0L }, Add = new[] { tagName } };
			var result = this.wiki.Tag(input);
			this.Assert(result.Count == 1, "Unexpected number of results returned: " + result.Count.ToString());
			if (result.Count == 1)
			{
				this.Assert(result[0].Status == "success", "Apply tagging failed.");
				this.Assert(result[1].Error.Code == "nosuchrevid", "Didn't get the expected error message.");
			}

			result = this.wiki.Tag(input);
			this.Assert(result[0].NoOperation, "Result was not NoOperation.");

			input = new TagInput() { RevisionIds = new[] { editResult.NewRevisionId }, Remove = new[] { tagName } };
			result = this.wiki.Tag(input);
			this.Assert(result[0].Removed.Count == 1, "Incorrect number of tags removed.");
			this.ShowStopwatch();
		}

		private void TokenTests()
		{
			this.StartStopwatch("Tokens");
			var token = this.wiki.TokenManager.SessionToken("csrf");
			if (token == null)
			{
				this.AppendResults("No CSRF token available");
			}

			this.ShowStopwatch();
		}

		private void UndeleteTests()
		{
			const string pageName = "Test Undelete";
			this.Edit(pageName, "Please don't delete this page! " + DateTime.UtcNow.ToString(), "Delete this page!");
			this.Delete(pageName, "Confusing - delete the page");

			this.StartStopwatch("Undelete");
			var result = this.adminWiki.Undelete(new UndeleteInput(pageName) { Reason = "Confusing - undelete the page" });
			this.Assert(result.Revisions > 0, "Undelete didn't undelete anything!");
			this.ShowStopwatch();
		}

		private void UserInfoTests()
		{
			this.StartStopwatch("UserInfo");
			var input = new UserInfoInput { Properties = UserInfoProperties.All };
			var result = this.wiki.UserInfo(input);
			if (result.Id == 0)
			{
				this.AppendResults("Failed to get UserId");
			}

			if (result.Name != this.wiki.UserName)
			{
				this.AppendResults("Bot name does not match UserInfo name");
			}

			this.CheckCollection(result.Groups, "Groups");
			this.CheckCollection(result.ImplicitGroups, "ImplicitGroups");
			this.CheckCollection(result.Options, "Options");
			this.CheckCollection(result.Rights, "Rights");

			if (this.adminWiki != null)
			{
				input = new UserInfoInput { Properties = UserInfoProperties.All };
				result = this.adminWiki.UserInfo(input);
				if (result.Id == 0)
				{
					this.AppendResults("Failed to get Admin UserId");
				}

				if (result.Name != this.adminWiki.UserName)
				{
					this.AppendResults("Admin name does not match UserInfo name");
				}
			}

			this.ShowStopwatch();
		}

		private void UserRightsTests()
		{
			var input = new UserRightsInput(this.wiki.UserName) { Add = new[] { "bot" }, Reason = "I am a bot!" };
			var result = this.adminWiki.UserRights(input);
			this.Assert(result.Added.Count > 0 && result.Added[0] == "bot", "Bot right was not added.");

			input = new UserRightsInput(this.wiki.UserName) { Remove = new[] { "bot" }, Reason = "I am NOT a bot!" };
			result = this.adminWiki.UserRights(input);
			this.Assert(result.Removed.Count > 0 && result.Removed[0] == "bot", "Bot right was not added.");
		}

		private void WatchTests()
		{
			this.StartStopwatch("Watch");
			var input = new WatchInput(new[] { "Main Page", "Main Page2" }) { Unwatch = true };
			var result = this.wiki.Watch(input);
			if (result.Count == 2)
			{
				this.Assert(result["Main Page"].Flags.HasFlag(WatchFlags.Unwatched), "Incorect flags value for Main Page.");
				this.Assert(result["Main Page2"].Flags.HasFlag(WatchFlags.Missing), "Incorrect flags value for missing Main Page2");
			}
			else
			{
				this.Assert(true, "Incorrect number of results.");
			}

			this.ShowStopwatch();
		}
		#endregion
	}
}
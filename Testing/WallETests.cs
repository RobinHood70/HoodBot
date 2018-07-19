namespace RobinHood70.Testing
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon;
	using static RobinHood70.Testing.TestingCommon;
	using static RobinHood70.WikiCommon.Globals;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Coupling is a necessity of the fact that we're using classes for inputs.")]
	public class WallETests : TestRunner, ITestRunner
	{
		#region Fields
		private WikiAbstractionLayer adminWiki;
		private WikiAbstractionLayer normalWiki;

		private LoginInput reLoginInput;
		#endregion

		#region Constructors
		public WallETests(ITestForm parentForm, WikiInfo wikiInfo)
			: base(parentForm, wikiInfo)
		{
		}
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

		#region Public Override Methods
		public void Relogin()
		{
			var result = this.normalWiki.Login(this.reLoginInput);
			if (result.UserId == 0)
			{
				throw new InvalidOperationException("User login failed: " + result.Reason);
			}
		}

		public override void Teardown()
		{
			if (this.adminWiki != null && !string.IsNullOrEmpty(this.WikiInfo.SecretKey))
			{
				RunJobs(this.adminWiki, this.WikiInfo.SecretKey);
			}

			this.normalWiki = null;
			this.adminWiki = null;
		}

		public override void RunOne()
		{
		}

		public override void RunAll()
		{
			if (this.normalWiki.Uri.Host == "rob-centos")
			{
				this.LoginTests();
			}

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
		}

		public override void Setup()
		{
			this.reLoginInput = new LoginInput(this.WikiInfo.UserName, this.WikiInfo.Password);
			this.normalWiki = this.SetupWal(this.WikiInfo, false);
			if (this.WikiInfo.AdminUserName != null)
			{
				this.adminWiki = this.SetupWal(this.WikiInfo, true);
			}
		}
		#endregion

		#region Private Methods
		private void AssertBeforeVersion(int version, bool condition, string message)
		{
			if (this.normalWiki.SiteVersion < version)
			{
				this.Assert(condition, message);
			}
		}

		private void AssertBetweenVersions(int minVersion, int maxVersion, bool condition, string message)
		{
			if (this.normalWiki.SiteVersion >= minVersion && this.normalWiki.SiteVersion <= maxVersion)
			{
				this.Assert(condition, message);
			}
		}

		private void AssertVersion(int version, bool condition, string message)
		{
			if (this.normalWiki.SiteVersion >= version)
			{
				this.Assert(condition, message);
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
					this.ParentForm.AppendResults("No pages in output");
				}
				else
				{
					this.CheckCollection(pages.First().Revisions, "Revisions");
				}
			}
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

		private EditResult Edit(string title, string content, string summary) => this.normalWiki.Edit(new EditInput(title, content) { Summary = summary });

		private EditResult EditAdmin(string title, string content, string summary) => this.adminWiki.Edit(new EditInput(title, content) { Summary = summary });

		private WikiAbstractionLayer SetupWal(WikiInfo wikiInfo, bool useAdmin)
		{
			var wal = GetAbstractionLayer(this.WikiInfo, useAdmin);
			wal.SendingRequest += DebugShowRequest;
			// wal.WarningOccurred += DebugWarningEventHandler;
			// wal.ResponseReceived += DebugResponseEventHandler;
			wal.Login(useAdmin ? new LoginInput(this.WikiInfo.AdminUserName, this.WikiInfo.AdminPassword) : this.reLoginInput);

			return wal;
		}

		private void TeardownWal(WikiAbstractionLayer wal) =>
			// wal.ResponseReceived -= DebugResponseEventHandler;
			// wal.WarningOccurred -= DebugWarningEventHandler;
			wal.SendingRequest -= DebugShowRequest;

		private void UploadRandomImage(string destinationName)
		{
			if (this.normalWiki.Uri.Host != "rob-centos")
			{
				throw new InvalidOperationException("You're uploading porn to a wiki that's not yours, dumbass!");
			}

			var rand = new Random();
			var files = Directory.GetFiles(@"C:\Users\rmorl\Pictures\Screen Saver Pics\", "*.jpg"); // Only select from jpgs so we don't have to worry about extension type.
			var fileName = files[rand.Next(files.Length)];

			using (var upload = new FileStream(fileName, FileMode.Open))
			{
				this.normalWiki.Upload(new UploadInput(destinationName, upload)
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
			this.ParentForm.StartStopwatch("AllImages");
			var input = new AllImagesInput();
			var result = this.normalWiki.AllImages(input);
			var pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);
			this.ParentForm.ShowStopwatch();
		}

		private void AllMessagesTests()
		{
			const string Message = "about";

			this.ParentForm.StartStopwatch("AllMessages");
			var input = new AllMessagesInput() { Messages = new List<string>() { Message }, LanguageCode = "bpy", IncludeLocal = true };
			var result = this.normalWiki.AllMessages(input);
			var resultFirst = result[0];
			if (resultFirst.Flags.HasFlag(MessageFlags.Missing))
			{
				this.ParentForm.AppendResults($"Failed to get '{Message}' message");
			}

			if (resultFirst.Content != "বারে")
			{
				this.ParentForm.AppendResults("Text mismatch");
			}

			input = new AllMessagesInput() { MessageFrom = "about", MessageTo = "aboutsite", LanguageCode = "en", IncludeLocal = true };
			result = this.normalWiki.AllMessages(input);
			if (result.Count == 0)
			{
				this.ParentForm.AppendResults("message range had no messages");
			}

			this.ParentForm.ShowStopwatch();
		}

		private void AllFileUsagesTests()
		{
			this.ParentForm.StartStopwatch("AllFileUsages");
			var input = new AllFileUsagesInput();
			var result = this.normalWiki.AllFileUsages(input);
			var pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
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
			this.ParentForm.ShowStopwatch();
		}

		private void AllLinksTest()
		{
			this.ParentForm.StartStopwatch("AllLinks");
			var input = new AllLinksInput();
			var result = this.normalWiki.AllLinks(input);
			var pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
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
			this.ParentForm.ShowStopwatch();
		}

		private void AllRedirectsTest()
		{
			this.ParentForm.StartStopwatch("AllRedirects");
			var input = new AllRedirectsInput();
			var result = this.normalWiki.AllRedirects(input);
			var pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
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
			this.ParentForm.ShowStopwatch();
		}

		private void AllTransclusionsTest()
		{
			this.ParentForm.StartStopwatch("AllTransclusions");
			var input = new AllTransclusionsInput() { Namespace = 0 };
			var result = this.normalWiki.AllTransclusions(input);
			var pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);
			this.ParentForm.ShowStopwatch();
		}

		private void BacklinksTests()
		{
			this.Edit("Test Page 1", "{{File:Test Image 1.jpg}}[[:File:Test Image 1.jpg]][[File:Test Image 1.jpg|thumb|Test image]]\n{{:Test Page 2}}\n\n[[Category:Test Pages]]", "Create/modify test page");
			this.Edit("Test Page 2", "[[Test Page 1]]\n\n[[Category:Test Pages]]", "Create/modify test page");
			this.Edit("Category:Test Images", "This is the category for test images.", "Create category");
			this.Edit("Category:Test Pages", "This is the category for test pages.", "Create category");
			this.Edit("Test Redirect 1", "#REDIRECT [[Test Page 1]]", "Create redirect");
			this.UploadRandomImage("Test Image 1.jpg");
			this.ParentForm.StartStopwatch("Backlinks");
			var input = new BacklinksInput("Test Page 1", BacklinksTypes.Backlinks) { FilterRedirects = Filter.Any };
			var result = this.normalWiki.Backlinks(input);
			var pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);

			input = new BacklinksInput("File:Test Image 1.jpg", BacklinksTypes.EmbeddedIn) { FilterRedirects = Filter.Any };
			result = this.normalWiki.Backlinks(input);
			pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);

			input = new BacklinksInput("File:Test Image 1.jpg", BacklinksTypes.ImageUsage) { FilterRedirects = Filter.Any };
			result = this.normalWiki.Backlinks(input);
			pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);

			input = new BacklinksInput("File:Test Image 1.jpg", BacklinksTypes.All) { FilterRedirects = Filter.Any };
			result = this.normalWiki.Backlinks(input);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.ParentForm.ShowStopwatch();
		}

		private void CategoriesTests()
		{
			this.ParentForm.StartStopwatch("Categories");
			var input = new AllCategoriesInput() { From = "Test", To = "Tesz" };
			var result = this.normalWiki.AllCategories(input);
			var pages = this.normalWiki.LoadPages(new QueryPageSetInput(input), DefaultPageProperties);
			this.CheckCollection(result, "result");
			if (result.Count > 0)
			{
				this.CheckForNull(result[0].Title, "Title");
			}

			this.CheckPagesResult(pages);
			this.ParentForm.ShowStopwatch();
		}

		private void EmailUserTests()
		{
			this.ParentForm.StartStopwatch("Email");
			var input = new EmailUserInput("RobinHood70", "This is a test.") { Subject = "Test Subject", CCMe = true };
			try
			{
				var result = this.normalWiki.EmailUser(input);
				this.Assert(result.Result == "Success", "E-mail user failed.");
			}
			catch (WikiException ex)
			{
				this.Assert(true, ex.Message);
			}

			this.ParentForm.ShowStopwatch();
		}

		private void ImageRotateTests()
		{
			const string imageName = "Test Image 1.jpg";
			this.UploadRandomImage(imageName);

			this.ParentForm.StartStopwatch("ImageRotate");
			var input = new ImageRotateInput(new[] { imageName }, 90);
			var result = this.normalWiki.ImageRotate(input);
			this.Assert(result.Count == 1, "Incorrect number of results.");
			this.ParentForm.ShowStopwatch();
		}

		private void ImportTests()
		{
			this.Delete("IngameEnding.as", "Remove previous test.");

			this.ParentForm.StartStopwatch("Import");
			var input = new ImportInput() { Xml = File.ReadAllText(@"D:\Data\WallE\Wiki26-20161214202753.xml"), FullHistory = true, Summary = "Test Import" };
			var result = this.adminWiki.Import(input);
			this.Assert(result.Count > 0, "No results after import.");
			this.Assert(result[0].Invalid == false, "Import result was invalid.");
			this.Assert(result[0].Revisions > 0, "Import revision count is 0.");
			this.ParentForm.ShowStopwatch();
		}

		private void LoginTests()
		{
			this.normalWiki.Logout(); // Since we're logged in by default now, specifically log out for these tests.
			this.ParentForm.StartStopwatch("Login");
			var input = new LoginInput(this.reLoginInput.UserName, "Bad password");
			var result = this.normalWiki.Login(input);
			if (result.Result != "WrongPass" && result.Result != "Failed")
			{
				this.ParentForm.AppendResults($"Login did not detect an incorrect password. User: {result.User}");
			}

			input = new LoginInput("Nobody", "this will fail");
			result = this.normalWiki.Login(input);
			if (result.Result != "NotExists" && result.Result != "Failed")
			{
				this.ParentForm.AppendResults($"Login did not detect a non-existent user. User: {result.User}");
			}

			this.ParentForm.ShowStopwatch();
			this.Relogin();
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

			this.ParentForm.StartStopwatch("ManageTags");
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

			this.ParentForm.ShowStopwatch();
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

			this.ParentForm.StartStopwatch("Merge");
			var input = new MergeHistoryInput(testPage1, testPage2)
			{
				Reason = "Testing",
			};
			this.adminWiki.MergeHistory(input);
			var revisionsInput = new RevisionsInput() { MaxItems = 3 };
			var pageSetInput = new QueryPageSetInput(new string[] { testPage2 });
			var pageResult = this.normalWiki.LoadPages(pageSetInput, new IPropertyInput[] { revisionsInput });
			this.Assert(pageResult.Count == 1, "Incorrect number of pages loaded.");
			this.Assert(pageResult.First().Revisions.Count == 2, "Incorrect number of revisions loaded.");
			this.ParentForm.ShowStopwatch();
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

			this.ParentForm.StartStopwatch("Move");
			var input = new MoveInput("User:" + pageNameWrong, "User:" + pageNameRight)
			{
				MoveSubpages = true,
				MoveTalk = true,
				NoRedirect = true,
				Reason = "Test move",
			};

			var result = this.normalWiki.Move(input);
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

			this.ParentForm.ShowStopwatch();
		}

		private void OptionsTests()
		{
			this.ParentForm.StartStopwatch("Options");
			var input = new OptionsInput();
			var change = new Dictionary<string, string>
			{
				["ccmeonemails"] = "false",
			};
			input.Change = change;
			this.normalWiki.Options(input);

			change["ccmeonemails"] = "true";
			this.normalWiki.Options(input);
			this.ParentForm.ShowStopwatch();
		}

		private void ParameterInfoTests()
		{
			this.ParentForm.StartStopwatch("ParameterInfo");
			var input = new ParameterInfoInput(new string[] { "block", "clientlogin", "expandtemplates", "main", "query", "query+deletedrevs" }) { HelpFormat = HelpFormat.Raw };
			var result = this.normalWiki.ParameterInfo(input);
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
			this.ParentForm.ShowStopwatch();
		}

		private void ParseTests()
		{
			this.ParentForm.StartStopwatch("Parse");
			var input = ParseInput.FromText("{{Test}} Hello {{subst:Test}} [[Category:Test]] [[File:Test.jpg|64px]] [[Main Page]]");
			input.ContentModel = "wikitext";
			input.Properties = ParseProperties.All;
			var result = this.normalWiki.Parse(input);
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
			this.ParentForm.ShowStopwatch();
		}

		private void PatrolTests()
		{
			var editResult = this.Edit("Test Patrol 1", Invariant($"Test page: {DateTime.UtcNow}"), "Test edit");
			var recentChangesResult = this.normalWiki.RecentChanges(new RecentChangesInput() { MaxItems = 1, Properties = RecentChangesProperties.Ids, User = this.normalWiki.UserName });
			var rcid = recentChangesResult[0].Id;

			this.ParentForm.StartStopwatch("Patrol");
			var input = this.normalWiki.SiteVersion < 122 ? new PatrolInput(rcid) : PatrolInput.FromRevisionId(editResult.NewRevisionId);
			var result = this.adminWiki.Patrol(input);
			this.Assert(result.RecentChangesId == rcid, "Returned rcid didn't match expected rcid.");
			this.ParentForm.ShowStopwatch();
		}

		private void ProtectTests()
		{
			this.Edit("Test Protect 1", Invariant($"Protected page: {DateTime.UtcNow}"), "Test edit");
			this.ParentForm.StartStopwatch("Protect");
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
			this.ParentForm.ShowStopwatch();
		}

		private void PurgeTests()
		{
			this.ParentForm.StartStopwatch("Purge");
			var input = new PurgeInput(new[] { "Main Page" });
			var result = this.normalWiki.Purge(input);
			this.Assert(result.Count == 1, "Purge returned the wrong number of results.");
			this.ParentForm.ShowStopwatch();
		}

		private void RevisionDeleteTests()
		{
			this.Edit("Test RevisionDelete", "Test deletable: " + DateTime.UtcNow.ToStringInvariant(), "This should get revdel'd");
			var editResult = this.Edit("Test RevisionDelete", "Test keepable: " + DateTime.UtcNow.ToStringInvariant(), "This should stay");

			this.ParentForm.StartStopwatch("RevisionDelete");
			var input = new RevisionDeleteInput(RevisionDeleteType.Revision, new long[] { editResult.OldRevisionId }) { Hide = RevisionDeleteProperties.All, Reason = "Because" };
			var result = this.adminWiki.RevisionDelete(input);

			this.Assert(result.Status == "Success", "Hide revisions was not successful.");
			this.Assert(result[0].Id == editResult.OldRevisionId, "Ids didn't match.");
			this.ParentForm.ShowStopwatch();
		}

		private void RollbackTests()
		{
			this.EditAdmin("Test Rollback", "Legitimate edit", "I am a good person");
			this.Edit("Test Rollback", "Vandalism edit 1", "I am vandalizing ur wiki");
			this.Edit("Test Rollback", "Vandalism edit 2", "I am vandalizing ur wiki");
			this.Edit("Test Rollback", "Vandalism edit 3", "I am vandalizing ur wiki");

			this.ParentForm.StartStopwatch("Rollback");
			var input = new RollbackInput("Test Rollback", this.normalWiki.UserName) { Summary = "Rollback test vandalism" };
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

			this.ParentForm.ShowStopwatch();
		}

		private void RsdTests()
		{
			this.ParentForm.StartStopwatch("Rsd");
			var result = this.normalWiki.Rsd();
			this.Assert(result.StartsWith("<?xml", StringComparison.Ordinal), "Rsd result was not XML.");
			this.ParentForm.ShowStopwatch();
		}

		private void ResetPasswordTests()
		{
			this.ParentForm.StartStopwatch("ResetPassword");
			var input = ResetPasswordInput.FromEmail("robinhood70@live.ca");
			input.Capture = true;
			var result = this.adminWiki.ResetPassword(input);
			this.CheckForNull(result.Status, nameof(result.Status));
			this.CheckCollection(result.Passwords, nameof(result.Passwords));
			this.ParentForm.ShowStopwatch();
		}

		private void SetNotificationTimestampTests()
		{
			this.ParentForm.StartStopwatch("SetNotificationTimestamp");

			// Full watchlist version
			var input = new SetNotificationTimestampInput() { Timestamp = DateTime.Now - TimeSpan.FromHours(1) };
			var result = this.normalWiki.SetNotificationTimestamp(input);
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
			result = this.normalWiki.SetNotificationTimestamp(input);
			this.Assert(result.Count == 2, "Incorrect number of pages returned");
			this.ParentForm.ShowStopwatch();
		}

		private void SiteInfoTests()
		{
			this.ParentForm.StartStopwatch("SiteInfo");
			var input = new SiteInfoInput { Properties = SiteInfoProperties.All };
			var result = this.normalWiki.SiteInfo(input);
			this.CheckForNull(result.BasePage, "BasePage");
			this.CheckForNull(result.MainPage, "MainPage");
			this.CheckForNull(result.SiteName, "SiteName");
			this.CheckForNull(result.Generator, "Generator");
			this.CheckCollection(result.LagInfo, "LagInfo");
			this.CheckCollection(result.MagicWords, "MagicWords");
			this.CheckCollection(result.NamespaceAliases, "NamespaceAliases");
			this.CheckCollection(result.Namespaces, "Namespaces");
			this.ParentForm.ShowStopwatch();
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
			var editResult = this.normalWiki.Edit(editInput);

			this.ParentForm.StartStopwatch("Tags");
			var input = new TagInput() { RevisionIds = new[] { editResult.NewRevisionId, 0L }, Add = new[] { tagName } };
			var result = this.normalWiki.Tag(input);
			this.Assert(result.Count == 1, "Unexpected number of results returned: " + result.Count.ToString());
			if (result.Count == 1)
			{
				this.Assert(result[0].Status == "success", "Apply tagging failed.");
				this.Assert(result[1].Error.Code == "nosuchrevid", "Didn't get the expected error message.");
			}

			result = this.normalWiki.Tag(input);
			this.Assert(result[0].NoOperation, "Result was not NoOperation.");

			input = new TagInput() { RevisionIds = new[] { editResult.NewRevisionId }, Remove = new[] { tagName } };
			result = this.normalWiki.Tag(input);
			this.Assert(result[0].Removed.Count == 1, "Incorrect number of tags removed.");
			this.ParentForm.ShowStopwatch();
		}

		private void TokenTests()
		{
			this.ParentForm.StartStopwatch("Tokens");
			var token = this.normalWiki.TokenManager.SessionToken("csrf");
			if (token == null)
			{
				this.ParentForm.AppendResults("No CSRF token available");
			}

			this.ParentForm.ShowStopwatch();
		}

		private void UndeleteTests()
		{
			const string pageName = "Test Undelete";
			this.Edit(pageName, "Please don't delete this page! " + DateTime.UtcNow.ToString(), "Delete this page!");
			this.Delete(pageName, "Confusing - delete the page");

			this.ParentForm.StartStopwatch("Undelete");
			var result = this.adminWiki.Undelete(new UndeleteInput(pageName) { Reason = "Confusing - undelete the page" });
			this.Assert(result.Revisions > 0, "Undelete didn't undelete anything!");
			this.ParentForm.ShowStopwatch();
		}

		private void UserInfoTests()
		{
			this.ParentForm.StartStopwatch("UserInfo");
			var input = new UserInfoInput { Properties = UserInfoProperties.All };
			var result = this.normalWiki.UserInfo(input);
			if (result.Id == 0)
			{
				this.ParentForm.AppendResults("Failed to get UserId");
			}

			if (result.Name != this.normalWiki.UserName)
			{
				this.ParentForm.AppendResults("Bot name does not match UserInfo name");
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
					this.ParentForm.AppendResults("Failed to get Admin UserId");
				}

				if (result.Name != this.adminWiki.UserName)
				{
					this.ParentForm.AppendResults("Admin name does not match UserInfo name");
				}
			}

			this.ParentForm.ShowStopwatch();
		}

		private void UserRightsTests()
		{
			var input = new UserRightsInput(this.normalWiki.UserName) { Add = new[] { "bot" }, Reason = "I am a bot!" };
			var result = this.adminWiki.UserRights(input);
			this.Assert(result.Added.Count > 0 && result.Added[0] == "bot", "Bot right was not added.");

			input = new UserRightsInput(this.normalWiki.UserName) { Remove = new[] { "bot" }, Reason = "I am NOT a bot!" };
			result = this.adminWiki.UserRights(input);
			this.Assert(result.Removed.Count > 0 && result.Removed[0] == "bot", "Bot right was not added.");
		}

		private void WatchTests()
		{
			this.ParentForm.StartStopwatch("Watch");
			var input = new WatchInput(new[] { "Main Page", "Main Page2" }) { Unwatch = true };
			var result = this.normalWiki.Watch(input);
			if (result.Count == 2)
			{
				this.Assert(result["Main Page"].Flags.HasFlag(WatchFlags.Unwatched), "Incorect flags value for Main Page.");
				this.Assert(result["Main Page2"].Flags.HasFlag(WatchFlags.Missing), "Incorrect flags value for missing Main Page2");
			}
			else
			{
				this.Assert(true, "Incorrect number of results.");
			}

			this.ParentForm.ShowStopwatch();
		}
		#endregion
	}
}

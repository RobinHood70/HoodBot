#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static Properties.EveMessages;
	using static RobinHood70.Globals;

	public class ActionQuery : ActionModulePageSet<QueryInput, PageItem>, IQueryPageSet
	{
		#region Fields
		private Func<PageItem> pageItemFactory;
		private MetaUserInfo userModule;
		#endregion

		#region Constructors
		public ActionQuery(WikiAbstractionLayer wal)
			: base(wal)
		{
		}

		public ActionQuery(WikiAbstractionLayer wal, Func<PageItem> pageFactory)
			: this(wal) => this.pageItemFactory = pageFactory;
		#endregion

		#region Public Properties
		public HashSet<string> DisabledModules { get; } = new HashSet<string>();

		public QueryInput Input { get; private set; }

		public int ItemsRemaining { get; set; }
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "query";
		#endregion

		#region Protected Override Properties
		protected override bool Continues
		{
			get
			{
				var continueParsing = this.ItemsRemaining != 0 || !this.ContinueModule.BatchComplete;
				if (continueParsing)
				{
					// Little point in short-circuiting this beyond the initial check since module count will be small, so just run through all of them.
					foreach (var module in this.Input.AllModules)
					{
						continueParsing &= module.ContinueParsing;
					}
				}

				return continueParsing;
			}
		}

		protected override int CurrentListSize
		{
			get
			{
				if (this.ItemsRemaining == int.MaxValue || this.ItemsRemaining + 10 > this.MaximumListSize)
				{
					return this.MaximumListSize;
				}
				else
				{
					return this.ItemsRemaining <= 5 ? 10 : this.ItemsRemaining + 10;
				}
			}
		}

		protected override IList<PageItem> Pages { get; } = new KeyedPages();

		protected override RequestType RequestType { get; } = RequestType.Get;

		protected override StopCheckMethods StopMethods => this.Wal.UserId == 0 ? this.Wal.StopCheckMethods & (StopCheckMethods.Custom | StopCheckMethods.TalkCheckQuery) : this.Wal.StopCheckMethods;
		#endregion

		#region Public Methods
		public void SubmitContinued(QueryInput input)
		{
			this.Wal.ClearWarnings();
			this.ContinueModule = this.Wal.ModuleFactory.CreateContinue();
			this.BeforeSubmit(input);
			this.SubmitInternal(input);
			while (this.ContinueModule.Continues && this.Continues)
			{
				this.SubmitInternal(input);
			}

			this.AfterSubmit();
		}
		#endregion

		#region Protected Override Methods
		protected override void AddWarning(string from, string text)
		{
			if (text != null)
			{
				if (text.StartsWith("Action '", StringComparison.Ordinal) && text.EndsWith("' is not allowed for the current user", StringComparison.Ordinal))
				{
					// Swallow all token warnings
					return;
				}

				// Originally, this only handled module-specific warnings, but it was changed to have all modules check all warnings to account for cases like MetaSiteInfo, which can generate a formatversion warning during its first call that originates from "main".
				if (this.HandleWarning(from, text))
				{
					return;
				}

				foreach (var module in this.Input.Modules)
				{
					if (module.HandleWarning(from, text))
					{
						return;
					}
				}
			}

			base.AddWarning(from, text);
		}

		protected override void AfterSubmit()
		{
			if (this.Wal.BreakRecursionAfterSubmit)
			{
				// Necessary because the custom stop check would become recursive if it called on any other modules.
				return;
			}

			this.Wal.BreakRecursionAfterSubmit = true;
			if (this.StopMethods.HasFlag(StopCheckMethods.Custom) && (this.Wal.CustomStopCheck?.Invoke() == true))
			{
				this.Wal.BreakRecursionAfterSubmit = false;
				throw new StopException(CustomStopCheckFailed);
			}

			var userOutput = this.userModule.Output;
			if (this.StopMethods.HasFlag(StopCheckMethods.UserNameCheck) && this.SiteVersion < 128 && this.Wal.UserName != userOutput.Name)
			{
				this.Wal.BreakRecursionAfterSubmit = false;

				// Used to check if username has unexpectedly changed, indicating that the bot has been logged out (or conceivably logged in) unexpectedly.
				throw new StopException(UserNameChanged);
			}

			if (this.StopMethods.HasFlag(StopCheckMethods.TalkCheckQuery) && userOutput.Flags.HasFlag(UserInfoFlags.HasMessage))
			{
				this.Wal.BreakRecursionAfterSubmit = false;
				throw new StopException(TalkPageChanged);
			}

			this.Wal.BreakRecursionAfterSubmit = false;
		}

		protected override void BeforeSubmit(QueryInput input)
		{
			ThrowNull(input, nameof(input));
			base.BeforeSubmit(input);
			if (input.PropertyModules.TryGetItem("revisions", out PropRevisions revModule) && revModule.IsRevisionRange)
			{
				this.MaximumListSize = 1;
			}
			else
			{
				if (input.Limit > 0 && input.Limit < this.MaximumListSize)
				{
					this.MaximumListSize = input.Limit;
				}
			}

			this.ItemsRemaining = input.MaxItems == 0 ? int.MaxValue : input.MaxItems;

			this.CheckActiveModules(input);
			var newInput = new QueryInput(input) { GetInterwikiUrls = input.GetInterwikiUrls }; // Make a copy so we can modify it.
			if ((this.StopMethods.HasFlag(StopCheckMethods.UserNameCheck) && this.SiteVersion < 128) || this.StopMethods.HasFlag(StopCheckMethods.TalkCheckQuery))
			{
				UserInfoInput userInfoInput;
				bool useExisting;

				// If a MetaUserInfo module already exists, remove it (so as not to corrupt its input data) and replace it with a merged copy of ours and the original.
				if (newInput.Modules.TryGetItem("userinfo", out MetaUserInfo userInfo))
				{
					userInfoInput = userInfo.Input;
					useExisting = true;
				}
				else
				{
					userInfoInput = new UserInfoInput();
					useExisting = false;
				}

				userInfoInput.Properties |= UserInfoProperties.BlockInfo;
				if (this.StopMethods.HasFlag(StopCheckMethods.TalkCheckQuery))
				{
					userInfoInput.Properties |= UserInfoProperties.HasMsg;
				}

				userInfo = new MetaUserInfo(this.Wal, userInfoInput);
				this.userModule = userInfo;

				if (!useExisting)
				{
					newInput.Modules.Add(userInfo);
				}
			}

			this.Input = newInput;
		}

		protected override void BuildRequestPageSet(Request request, QueryInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			foreach (var module in this.Input.PropertyModules)
			{
				if (!this.DisabledModules.Contains(module.Name))
				{
					module.BuildRequest(request);
				}
			}

			foreach (var module in this.Input.Modules)
			{
				module.BuildRequest(request);
			}

			/*
			if (this.userModule != null && this.Wal.UserId > 0)
			{
				this.userModule.BuildRequest(request);
			}
			*/

			request.Add("iwurl", input.GetInterwikiUrls);
		}

		protected override void DeserializePage(JToken result, PageItem page)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(page, nameof(page));
			page.Flags =
				result.GetFlag("invalid", PageFlags.Invalid) |
				result.GetFlag("missing", PageFlags.Missing);
		}

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			base.DeserializeParent(parent);
			var modules = this.Input.Modules;
			var propModules = this.Input.PropertyModules;
			var limits = parent["limits"];
			if (limits != null)
			{
#pragma warning disable IDE0007 // Use implicit type
				foreach (JProperty limit in limits)
#pragma warning restore IDE0007 // Use implicit type
				{
					if (modules.TryGetItem(limit.Name, out var module))
					{
						module.ModuleLimit = (int)limit.Value;
					}
					else if (propModules.TryGetItem(limit.Name, out var propModule))
					{
						propModule.ModuleLimit = (int)limit.Value;
					}
				}
			}

			// Kludgey workaround for https://phabricator.wikimedia.org/T36356. If there had been more than just this one module, some sort of "Needs deserializing during parent's DeserializeParent" feature could have been added, but that seemed just as kludgey as this for a single faulty module.
			var watchlistRaw = parent[ListWatchlistRaw.ModuleName];
			if (watchlistRaw != null)
			{
				if (modules.TryGetItem(ListWatchlistRaw.ModuleName, out ListWatchlistRaw watchlistModule))
				{
					watchlistModule.Deserialize(parent);
				}
			}
		}

		protected override IReadOnlyList<PageItem> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			if (this.Input.PageSetQuery)
			{
				this.GetExceptions(result);
				var pages = result["pages"];
				if (pages != null)
				{
					this.DeserializePages(pages);
				}
			}

			foreach (var module in this.Input.Modules)
			{
				module.Deserialize(result);
			}

			return null;
		}
		#endregion

		#region Private Static Methods
		private static string FakeTitleFromId(long? pageId) => pageId == null ? null : "#" + ((long)pageId).ToStringInvariant();
		#endregion

		#region Private Methods
		private void CheckActiveModules(QueryInput input)
		{
			// This could be a pagset query that's just getting a list of pages and nothing else, so no need to check if we have property modules. If it isn't a pageset query, though, throw an error because "action=query" on its own is not useful, and is clearly an error in the input.
			if (input.Modules.Count == 0 && !input.PageSetQuery)
			{
				throw new InvalidOperationException(InvalidQuery);
			}

			if (input.Modules.Count > 0 || input.PropertyModules.Count > 0)
			{
				// Check if any modules are active. This is done before adding/merging the UserModule, since that would always make it appear that there's an active module.
				var hasActiveModule = false;
				foreach (var module in input.AllModules)
				{
					if (module.MinimumVersion == 0 || this.SiteVersion == 0 || this.SiteVersion >= module.MinimumVersion)
					{
						hasActiveModule = true;
					}
					else
					{
						this.Wal.AddWarning("query-modulenotsupported", module.GetType().Name);
					}
				}

				if (!hasActiveModule)
				{
					throw new InvalidOperationException(NoSupportedModules);
				}
			}
		}

		private void DeserializePages(JToken result)
		{
			ThrowNull(result, nameof(result));
			var pages = this.Pages as KeyedPages;
			foreach (var page in result)
			{
				var innerResult = this.Wal.DetectedFormatVersion == 2 ? page : page?.First;
				var pageName = (string)innerResult["title"];
				var pageId = (long?)innerResult["pageid"];
				var search = pageName ?? FakeTitleFromId(pageId);
				if (search == null)
				{
					// Some generators can return missing pages with no title (or id?), most commonly when links tables are out of date and need refreshLinks.php run on them. If we get one of these, skip to the next page.
					// Unsure if page id is also not returned, so switching to a throw for now rather than skipping.
					throw new FormatException();

					// return;
				}

				if (!pages.TryGetItem(search, out var item))
				{
					if (this.ItemsRemaining == 0)
					{
						// If we've hit our limit, stop creating new pages, but we still need to check existing ones in case they're continued pages from previous results.
						continue;
					}

					item = this.pageItemFactory();
					this.DeserializeTitle(innerResult, item);
					pages.Add(item);
					if (this.ItemsRemaining != int.MaxValue)
					{
						this.ItemsRemaining--;
					}
				}

				foreach (var module in this.Input.PropertyModules)
				{
					module.SetPageOutput(item);
					module.Deserialize(innerResult);
				}
			}
		}
		#endregion

		#region Private Classes
		private class KeyedPages : KeyedCollection<string, PageItem>
		{
			#region Public Methods
			public bool TryGetItem(string key, out PageItem item)
			{
				if (this.Dictionary != null)
				{
					return this.Dictionary.TryGetValue(key, out item);
				}

				foreach (var testItem in this)
				{
					if (this.GetKeyForItem(testItem) == key)
					{
						item = testItem;
						return true;
					}
				}

				item = null;
				return false;
			}
			#endregion

			#region Public Override Methods
			protected override string GetKeyForItem(PageItem item) => item?.Title;
			#endregion
		}
		#endregion
	}
}
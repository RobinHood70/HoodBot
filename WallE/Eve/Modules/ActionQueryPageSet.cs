#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics.CodeAnalysis;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// While there's some code overlap between this and ActionQuery, having the two as separate entities significantly reduces the code complexity in ActionQuery, and in this as well, to a lesser extent.
	internal class ActionQueryPageSet : ActionModulePageSet<QueryInput, PageItem>, IPageSetGenerator
	{
		#region Fields
		private readonly QueryInput input;
		private readonly TitleCreator<PageItem>? pageFactory;
		private readonly MetaUserInfo? userModule;
		private int itemsRemaining;
		#endregion

		#region Constructors
		public ActionQueryPageSet(WikiAbstractionLayer wal, QueryInput input, TitleCreator<PageItem> pageFactory)
			: base(wal)
		{
			ThrowNull(pageFactory, nameof(pageFactory));
			this.pageFactory = pageFactory;
			this.input = input;
			var props =
				((wal.ValidStopCheckMethods.HasFlag(StopCheckMethods.UserNameCheck) && wal.SiteVersion < 128) ? UserInfoProperties.BlockInfo : UserInfoProperties.None)
				| (wal.ValidStopCheckMethods.HasFlag(StopCheckMethods.TalkCheckQuery) ? UserInfoProperties.HasMsg : UserInfoProperties.None);
			if (props != UserInfoProperties.None)
			{
				var userInfoInput = new UserInfoInput() { Properties = props };
				this.userModule = new MetaUserInfo(wal, userInfoInput);
			}
		}
		#endregion

		#region Public Properties
		public HashSet<string> InactiveModules { get; } = new HashSet<string>();

		public UserInfoResult? UserInfo { get; protected set; }
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 0;

		public override string Name => "query";
		#endregion

		#region Protected Override Properties
		protected bool Continues
		{
			get
			{
				if (this.itemsRemaining > 0 || (this.ContinueModule != null && !this.ContinueModule.BatchComplete))
				{
					// Logic changed, cuz I think there was a bug in here previously. Used to be if any module didn't continue, the whole thing would return false. Pretty sure it should be if anything wants to continue, the whole thing should.
					var cont = false;
					var enumerator = this.AllModules.GetEnumerator();
					while (!cont && enumerator.MoveNext())
					{
						if (enumerator.Current is IContinuableQueryModule continuableModule)
						{
							cont |= continuableModule.ContinueParsing;
						}
					}

					return cont;
				}

				return false;
			}
		}

		protected override int CurrentListSize => (this.itemsRemaining == int.MaxValue || this.itemsRemaining + 10 > this.MaximumListSize)
			? this.MaximumListSize
			: (this.itemsRemaining <= 5 ? 10 : this.itemsRemaining + 10);

		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Private Properties
		private IEnumerable<IQueryModule> AllModules
		{
			get
			{
				foreach (var module in this.input.QueryModules)
				{
					yield return module;
				}

				foreach (var module in this.input.PropertyModules)
				{
					yield return module;
				}
			}
		}
		#endregion

		#region Public Methods
		public PageSetResult<PageItem> Submit()
		{
			this.MaximumListSize = this.Wal.MaximumPageSetSize;
			if (this.input.GeneratorInput != null)
			{
				this.Generator = this.Wal.ModuleFactory.CreateGenerator(this.input.GeneratorInput, this);
			}

			this.Wal.ClearWarnings();
			this.BeforeSubmit();
			this.ContinueModule = this.Wal.ModuleFactory.CreateContinue();
			this.ContinueModule.BeforePageSetSubmit(this);
			this.PageSetDone = false;

			var pages = new KeyedPages();
			do
			{
				var request = this.CreateRequest();
				var response = this.Wal.SendRequest(request);
				this.ParseResponse(response, pages);
				while (this.ContinueModule.Continues && this.Continues)
				{
					request = this.CreateRequest();
					response = this.Wal.SendRequest(request);
					this.ParseResponse(response, pages);
				}
			}
			while (!this.PageSetDone);

			return this.CreatePageSet(pages);
		}
		#endregion

		#region Public Override Methods
		[DoesNotReturn]
		public override PageSetResult<PageItem> Submit(QueryInput input) => throw new InvalidOperationException(CurrentCulture(EveMessages.UseSubmit, nameof(this.Submit)));
		#endregion

		#region Protected Override Methods
		protected override void BeforeSubmit()
		{
			base.BeforeSubmit();
			if (this.input.PropertyModules != null && this.input.PropertyModules.Find(module => module.Name == "revisions") is PropRevisions revModule && revModule.IsRevisionRange)
			{
				this.MaximumListSize = 1;
			}
			else if (this.input.Limit > 0 && this.input.Limit < this.MaximumListSize)
			{
				this.MaximumListSize = this.input.Limit;
			}

			this.itemsRemaining = this.input.MaxItems == 0 ? int.MaxValue : this.input.MaxItems;
			this.CheckActiveModules();
		}

		// Written this way just to make it obvious that in this case, the input is not being used, since this.input is the correct "parameter".
		protected override void BuildRequestPageSet(Request request, QueryInput input) => this.BuildRequestPageSet(request);

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			base.DeserializeParent(parent);
			if (parent["limits"] is JToken limits)
			{
				foreach (var limit in limits.Children<JProperty>())
				{
					var value = (int)limit.Value;
					if (this.Generator?.Name == limit.Name)
					{
						this.Generator.ModuleLimit = value;
					}

					foreach (var allModule in this.AllModules)
					{
						if (allModule.Name == limit.Name && allModule is IContinuableQueryModule module)
						{
							module.ModuleLimit = value;
							break;
						}
					}
				}
			}

			// Kludgey workaround for https://phabricator.wikimedia.org/T36356. If there had been more than just this one module, some sort of "Needs deserializing during parent's DeserializeParent" feature could have been added, but that seemed just as kludgey as this for a single faulty module.
			if (parent[ListWatchlistRaw.ModuleName] != null && this.input.QueryModules.Find(module => module.Name == ListWatchlistRaw.ModuleName) is ListWatchlistRaw watchListModule)
			{
				watchListModule.Deserialize(parent);
			}
		}

		protected override void DeserializeResult(JToken result, IList<PageItem> pages)
		{
			ThrowNull(result, nameof(result));
			this.GetPageSetNodes(result);
			if (result["pages"] is JToken pagesNode)
			{
				this.DeserializePages(pagesNode, pages);
			}

			foreach (var module in this.input.QueryModules)
			{
				module.Deserialize(result);
			}

			if (this.userModule != null)
			{
				this.userModule.Deserialize(result);
				this.UserInfo = this.userModule.Output;
			}
		}

		protected override PageItem GetItem(JToken result)
		{
			ThrowNull(result, nameof(result));
			if (this.pageFactory == null)
			{
				throw new InvalidOperationException(EveMessages.PageFactoryNotSet);
			}

			var page = this.pageFactory(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				pageId: (long?)result["pageid"] ?? 0);
			page.Flags = result.GetFlags(
				("invalid", PageFlags.Invalid),
				("missing", PageFlags.Missing));

			return page;
		}

		protected override bool HandleWarning(string from, string text)
		{
			if (text.StartsWith("Action '", StringComparison.Ordinal) && text.EndsWith("' is not allowed for the current user", StringComparison.Ordinal))
			{
				// Swallow all token warnings
				return true;
			}

			foreach (var module in this.input.QueryModules)
			{
				if (module.HandleWarning(from, text))
				{
					return true;
				}
			}

			return base.HandleWarning(from, text);
		}
		#endregion

		#region Private Methods
		private void BuildRequestPageSet(Request request)
		{
			ThrowNull(request, nameof(request));
			foreach (var module in this.input.PropertyModules)
			{
				if (!this.InactiveModules.Contains(module.Name))
				{
					module.BuildRequest(request);
				}
			}

			foreach (var module in this.input.QueryModules)
			{
				module.BuildRequest(request);
			}

			this.userModule?.BuildRequest(request);
			request.Add("iwurl", this.input.GetInterwikiUrls);
		}

		private void CheckActiveModules()
		{
			if (this.input.QueryModules.Count > 0 || this.input.PropertyModules.Count > 0)
			{
				// Check if any modules are active. This is done before adding/merging the UserModule, since that would always make it appear that there's an active module.
				var hasActiveModule = false;
				foreach (var module in this.AllModules)
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
					throw new InvalidOperationException(EveMessages.NoSupportedModules);
				}
			}
		}

		private Request CreateRequest()
		{
			var request = this.CreateBaseRequest();
			request.Prefix = this.Prefix;
			this.BuildRequest(request, this.input);
			request.Prefix = string.Empty;

			return request;
		}

		private void DeserializePages(JToken result, IList<PageItem> pagesIn)
		{
			ThrowNull(result, nameof(result));
			var pages = (KeyedPages)pagesIn;
			foreach (var page in result)
			{
				// Some generators can return missing pages with no title (or ID?), most commonly when links tables are out of date and need refreshLinks.php run on them. If we get one of these, skip to the next page.
				if (
					(this.Wal.DetectedFormatVersion > 1 ? page : page?.First) is JToken innerResult
					&& ((string?)innerResult["title"] ?? FakeTitleFromId((long?)innerResult["pageid"])) is string search)
				{
					var item = pages.ValueOrDefault(search);
					if (item == null && this.itemsRemaining > 0)
					{
						// If we've hit our limit, stop creating new pages, but we still need to check existing ones in case they're continued pages from previous results.
						item = this.GetItem(innerResult);
						pages.Add(item);
						if (this.itemsRemaining != int.MaxValue)
						{
							this.itemsRemaining--;
						}
					}

					if (item != null && this.input.PropertyModules is List<IPropertyModule> propModules)
					{
						foreach (var module in propModules)
						{
							module.Deserialize(innerResult, item);
						}
					}
				}
			}
		}
		#endregion

		#region Private Classes
		private class KeyedPages : KeyedCollection<string, PageItem>
		{
			#region Public Methods
			public PageItem? ValueOrDefault(string key)
			{
				if (key != null)
				{
					if (this.Dictionary != null)
					{
						return this.Dictionary.TryGetValue(key, out var item) ? item : default;
					}

					foreach (var testItem in this)
					{
						if (this.GetKeyForItem(testItem) == key)
						{
							return testItem;
						}
					}
				}

				return default;
			}
			#endregion

			#region Protected Override Methods
			protected override string GetKeyForItem(PageItem item) => item?.Title ?? Unknown;
			#endregion
		}
		#endregion
	}
}
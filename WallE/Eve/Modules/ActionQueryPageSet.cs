﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Properties;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

// While there's some code overlap between this and ActionQuery, having the two as separate entities significantly reduces the code complexity in ActionQuery, and in this as well, to a lesser extent.
internal sealed class ActionQueryPageSet : ActionModulePageSet<QueryInput, PageItem>
{
	#region Fields
	private readonly QueryInput input;
	private readonly TitleCreator<PageItem>? pageFactory;
	private readonly MetaUserInfo? userModule;
	private int pagesRemaining;
	#endregion

	#region Constructors
	public ActionQueryPageSet(WikiAbstractionLayer wal, QueryInput input, TitleCreator<PageItem> pageFactory)
		: base(wal)
	{
		ArgumentNullException.ThrowIfNull(pageFactory);
		this.pageFactory = pageFactory;
		this.input = input;
		var props =
			((wal.ValidStopCheckMethods.HasAnyFlag(StopCheckMethods.UserNameCheck) && wal.SiteVersion < 128) ? UserInfoProperties.BlockInfo : UserInfoProperties.None)
			| (wal.ValidStopCheckMethods.HasAnyFlag(StopCheckMethods.TalkCheckQuery) ? UserInfoProperties.HasMsg : UserInfoProperties.None);
		if (props != UserInfoProperties.None)
		{
			UserInfoInput userInfoInput = new() { Properties = props };
			this.userModule = new MetaUserInfo(wal, userInfoInput);
		}

		/* Below used to include the following instead of this.Wal.MaximumPageSetSize, but I don't believe it is correct or has any bearing on the results:
			this.input.GeneratorInput is ILimitableInput limitable &&
			limitable.Limit > 0 && limitable.Limit < this.Wal.MaximumPageSetSize
				? limitable.Limit
				: this.Wal.MaximumPageSetSize;
		*/
		this.MaximumListSize =
			this.input.PropertyModules?.Find(module => module.Name.OrdinalEquals("revisions")) is PropRevisions revModule &&
			revModule.IsRevisionRange
				? 1
				: this.Wal.MaximumPageSetSize;
	}
	#endregion

	#region Public Properties
	public HashSet<string> InactiveModules { get; } = new HashSet<string>(StringComparer.Ordinal);

	public UserInfoResult? UserInfo { get; private set; }
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 0;

	public override string Name => "query";
	#endregion

	#region Protected Override Properties
	protected override bool Continues
	{
		get
		{
			if (this.pagesRemaining > 0 || (this.ContinueModule?.BatchComplete == false))
			{
				var enumerator = this.AllModules.GetEnumerator();
				while (enumerator.MoveNext())
				{
					// CONSIDER: Does this make sense to check anymore? In most pageset queries, we won't care about number of sub-items. Original idea was for Revisions module, I think, but that can't work because we don't have the required info in the continuation data to forcibly fudge it. Very likely "ContinueParsing" should be removed or maybe only work in non-generator mode, if applicable.
					// If anything tells us not to continue parsing, bail out. This is most commonly used to abort a query when MaxItems has been reached.
					if (enumerator.Current is IContinuableQueryModule continuableModule && !continuableModule.ContinueParsing)
					{
						return false;
					}
				}

				return true;
			}

			return false;
		}
	}

	// The idea behind this is that some results may be disqualified or not returned (e.g., pages not found) when trying to reach a specific number of pages, so we always ask for a little extra in order to reduce the number of small queries.
	protected override int CurrentListSize =>
		this.pagesRemaining > this.MaximumListSize - 10
			? this.MaximumListSize
			: this.pagesRemaining switch
			{
				int.MaxValue => this.MaximumListSize,
				<= 5 => 10,
				_ => this.pagesRemaining + 10
			};

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
	public PageSetResult<PageItem> Submit() => this.Submit(this.input);
	#endregion

	#region Public Override Methods
	public override PageSetResult<PageItem> Submit(QueryInput input) => input != this.input
		? throw new InvalidOperationException(Globals.CurrentCulture(EveMessages.UseSubmit, nameof(this.Submit)))
		: base.Submit(input);
	#endregion

	#region Protected Override Methods
	protected override void BeforeSubmit()
	{
		base.BeforeSubmit();
		this.pagesRemaining = this.input.GeneratorInput is ILimitableInput limitable && limitable.MaxItems > 0 ? limitable.MaxItems : int.MaxValue;
		ActionQuery.CheckActiveModules(this.Wal, this.AllModules);
	}

	// Written this way just to make it obvious that in this case, the input is not being used, since this.input is the correct "parameter".
	protected override void BuildRequestPageSet(Request request, QueryInput input) => this.BuildRequestPageSet(request);

	[SuppressMessage("Usage", "IDE0028:Simplify collection initialization", Justification = "We want to create a KeyedPages here, not a generic list.")]
	protected override IList<PageItem> CreatePageList() => new KeyedPages();

	protected override void DeserializeActionExtra(JToken result)
	{
		ArgumentNullException.ThrowIfNull(result);
		base.DeserializeActionExtra(result);
		List<IQueryModule> list = [.. this.AllModules];
		if (this.Generator != null)
		{
			list.Add(this.Generator);
		}

		ActionQuery.CheckResult(result, list);
	}

	protected override void DeserializeResult(JToken result, IList<PageItem> pages)
	{
		ArgumentNullException.ThrowIfNull(result);
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
		ArgumentNullException.ThrowIfNull(result);
		if (this.pageFactory == null)
		{
			throw new InvalidOperationException(EveMessages.PageFactoryNotSet);
		}

		// Invalid titles can be missing a namespace. Since the page factory requires one, we use 0.
		return this.pageFactory(
			ns: (int?)result["ns"] ?? 0,
			title: result.MustHaveString("title"),
			pageId: (long?)result["pageid"] ?? 0,
			flags: result.GetFlags(
				("invalid", PageFlags.Invalid),
				("missing", PageFlags.Missing)));
	}

	protected override bool HandleWarning(string from, string text) => ActionQuery.HandleWarning(from, text, this.input.QueryModules, this.userModule) || base.HandleWarning(from, text);
	#endregion

	#region Private Methods
	private void BuildRequestPageSet(Request request)
	{
		ArgumentNullException.ThrowIfNull(request);
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

	private void DeserializePages(JToken result, IList<PageItem> pagesIn)
	{
		ArgumentNullException.ThrowIfNull(result);
		var pages = (KeyedPages)pagesIn;
		foreach (var page in result)
		{
			// Some generators can return missing pages with no title (or ID?), most commonly when links tables are out of date and need refreshLinks.php run on them. If we get one of these, skip to the next page.
			if ((this.Wal.DetectedFormatVersion > 1
					? page
					: page?.First) is JToken innerResult
				&& ((string?)innerResult["title"] ?? FakeTitleFromId((long?)innerResult["pageid"])) is string search)
			{
				var item = pages.ValueOrDefault(search);
				if (item == null && this.pagesRemaining > 0)
				{
					// If we've hit our limit, stop creating new pages, but we still need to check existing ones in case they're continued pages from previous results.
					item = this.GetItem(innerResult);
					pages.Add(item);
					if (this.pagesRemaining != int.MaxValue)
					{
						this.pagesRemaining--;
					}
				}

				if (item != null && this.input.PropertyModules is List<IPropertyModule> propModules)
				{
					foreach (var module in propModules)
					{
						module.Deserialize(innerResult);
						if (module.OutputObject is object output)
						{
							item.ParseModuleOutput(output);
						}
					}
				}
			}
		}
	}
	#endregion

	#region Private Classes
	private sealed class KeyedPages : KeyedCollection<string, PageItem>
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
					if (this.GetKeyForItem(testItem).OrdinalEquals(key))
					{
						return testItem;
					}
				}
			}

			return default;
		}
		#endregion

		#region Protected Override Methods
		protected override string GetKeyForItem(PageItem item) => item?.Title ?? Globals.Unknown;
		#endregion
	}
	#endregion
}
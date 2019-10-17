#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class ActionModulePageSet<TInput, TOutput> : ActionModule<TInput, IReadOnlyList<TOutput>>, IPageSetGenerator
		where TInput : PageSetInput
		where TOutput : ITitle
	{
		#region Static Fields
		private static readonly Regex TooManyFinder = new Regex(@"Too many values .*?'(?<parameter>.*?)'.*?limit is (?<sizelimit>[0-9]+)", RegexOptions.Compiled);
		#endregion

		#region Fields
		private readonly HashSet<long> badRevisionIds = new HashSet<long>();
		private readonly Dictionary<string, string> converted = new Dictionary<string, string>();
		private readonly Dictionary<string, InterwikiTitleItem> interwiki = new Dictionary<string, InterwikiTitleItem>();
		private readonly Dictionary<string, string> normalized = new Dictionary<string, string>();
		private readonly Dictionary<string, PageSetRedirectItem> redirects = new Dictionary<string, PageSetRedirectItem>();

		private bool done;
		private int offset;
		private List<string> values;
		#endregion

		#region Constructors
		protected ActionModulePageSet(WikiAbstractionLayer wal, TitleCreator<TOutput>? itemCreator)
			: base(wal) => this.ItemCreator = itemCreator;
		#endregion

		#region Public Properties
		public IGeneratorModule Generator { get; set; }
		#endregion

		#region Protected Properties
		protected ContinueModule ContinueModule { get; set; }

		protected int MaximumListSize { get; set; }

		protected TitleCreator<TOutput>? ItemCreator { get; }
		#endregion

		#region Protected Virtual Properties
		protected virtual int CurrentListSize => this.MaximumListSize;

		protected virtual IList<TOutput> Pages { get; } = new List<TOutput>();
		#endregion

		#region Public Methods
		public PageSetResult<TOutput> CreatePageSet() =>
			new PageSetResult<TOutput>(
				titles: this.Pages,
				badRevisionIds: new List<long>(this.badRevisionIds),
				converted: this.converted,
				interwiki: this.interwiki,
				normalized: this.normalized,
				redirects: this.redirects);

		public PageSetResult<TOutput> SubmitPageSet(TInput input)
		{
			ThrowNull(input, nameof(input));
			this.MaximumListSize = this.Wal.MaximumPageSetSize;
			this.values = new List<string>(input.Values ?? Array.Empty<string>());
			if (input.GeneratorInput != null)
			{
				this.Generator = this.Wal.ModuleFactory.CreateGenerator(input.GeneratorInput, this);
			}

			this.Wal.ClearWarnings();
			this.BeforeSubmit(input);
			this.ContinueModule = this.Wal.ModuleFactory.CreateContinue();
			this.ContinueModule.BeforePageSetSubmit(this);
			this.offset = 0;

			do
			{
				this.SubmitInternal(input);
				while (this.ContinueModule.Continues && this.Continues)
				{
					this.SubmitInternal(input);
				}
			}
			while (!this.done);

			this.AfterSubmit();
			return this.CreatePageSet();
		}
		#endregion

		#region Public Virtual Methods
		public virtual bool HandleWarning(string from, string text)
		{
			if (from == this.Name)
			{
				var match = TooManyFinder.Match(text);
				if (match.Success)
				{
					var parameter = match.Groups["parameter"].Value;
					if (PageSetInput.AllTypes.Contains(parameter))
					{
						this.done = false;
						this.MaximumListSize = int.Parse(match.Groups["sizelimit"].Value, CultureInfo.InvariantCulture);
						this.offset = this.MaximumListSize;
						return true;
					}
				}
			}

			return false;
		}
		#endregion

		#region Protected Static Methods
		protected static string FakeTitleFromId(long? pageId) => pageId.HasValue ? '#' + pageId.Value.ToStringInvariant() : null;
		#endregion

		#region Protected Methods
		protected void GetExceptions(JToken result)
		{
			ThrowNull(result, nameof(result));
			var node = result["badrevids"];
			if (node != null)
			{
				foreach (var item in node)
				{
					this.badRevisionIds.Add((long)item.First["revid"]);
				}
			}

			AddToDictionary(result["converted"], this.converted);
			var links = result["interwiki"].GetInterwikiLinks();
			foreach (var link in links)
			{
				this.interwiki.Add(link.Title, link);
			}

			AddToDictionary(result["normalized"], this.normalized);
			result["redirects"].GetRedirects(this.redirects, this.Wal);
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual WikiTitleItem DeserializeTitle(JToken result)
		{
			// TODO: I think DeserializeTitle can be merged into DeserializePage and just have that create and return the whole thing.
			ThrowNull(result, nameof(result));
			var ns = (int)result.NotNull("ns");
			var title = result.SafeString("title");
			var id = (int)result.NotNull("pageid");
			return new WikiTitleItem(ns, title, id);
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void BuildRequestPageSet(Request request, TInput input);

		protected abstract void DeserializePage(JToken result, TOutput page);
		#endregion

		#region Protected Override Methods
		protected override void AddWarning(string from, string text)
		{
			if (!this.HandleWarning(from, text))
			{
				base.AddWarning(from, text);
			}
		}

		protected override void BuildRequestLocal(Request request, TInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Prefix = string.Empty;
			if (this.Generator != null)
			{
				request.Add("generator", this.Generator.Name);
				this.Generator.BuildRequest(request);
			}

			if (this.values?.Count > 0)
			{
				var listSize = this.values.Count - this.offset;
				this.done = listSize <= this.CurrentListSize;
				if (!this.done)
				{
					listSize = this.CurrentListSize;
				}

				var currentGroup = this.values.GetRange(this.offset, listSize);

				// Several generators also use titles/pageids/revids, so emit them if present, whether or not there's a generator.
				request.Add(input.TypeName, currentGroup);
			}
			else
			{
				this.done = true;
			}

			request
				.AddIf("converttitles", input.ConvertTitles, input.GeneratorInput != null || input.ListType == ListType.Titles)
				.AddIf("redirects", input.Redirects, input.ListType != ListType.RevisionIds);

			this.BuildRequestPageSet(request, input);
			request.Prefix = string.Empty;
			this.ContinueModule?.BuildRequest(request);
		}

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			base.DeserializeParent(parent);
			if (this.ContinueModule != null)
			{
				var newVersion = this.ContinueModule.Deserialize(parent);
				if (newVersion != 0)
				{
					this.Wal.ContinueVersion = newVersion;
					this.ContinueModule = this.Wal.ModuleFactory.CreateContinue();
					this.ContinueModule.Deserialize(parent);
				}

				if (!this.done && !this.ContinueModule.BatchComplete && !this.ContinueModule.Continues)
				{
					this.offset += this.CurrentListSize;
				}
			}

			// This is a fugly workaround for the fact that modules other than queries will have the pageset data at the parent level, while query has it at the child level. I couldn't figure out a better way to do it (other than to simply ignore it and check for results that will never be there).
			if (!(this is ActionQuery))
			{
				this.GetExceptions(parent);
			}
		}

		protected override IReadOnlyList<TOutput>? DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			if (this.ItemCreator == null)
			{
				throw new InvalidOperationException("Trying to create pages with no page creator!");
			}

			foreach (var item in result)
			{
				var wikiTitle = this.DeserializeTitle(result);
				var output = this.ItemCreator(wikiTitle.Namespace, wikiTitle.Title, wikiTitle.PageId);
				this.DeserializePage(item, output);
			}

			// PageSets don't actually return a value here, instead returning it in Pages, so return null instead.
			// TODO: Could this be used as a better way to return the pageset information like redirects and invalid titles?
			return null;
		}
		#endregion

		#region Private Static Methods
		private static void AddToDictionary<TKey, TValue>(JToken token, IDictionary<TKey, TValue> dict)
		{
			if (token != null)
			{
				foreach (var item in token)
				{
					if (item["from"] != null)
					{
						var key = item["from"].Value<TKey>();
						var value = item["to"].Value<TValue>();
						dict.Add(key, value);
					}
				}
			}
		}
		#endregion

	}
}
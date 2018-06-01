#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using System.Globalization;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	public abstract class ActionModulePageSet<TInput, TOutput> : ActionModule<TInput, IReadOnlyList<TOutput>>, IPageSetInternal
		where TInput : PageSetInputBase
		where TOutput : ITitle, new()
	{
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
		protected ActionModulePageSet(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Properties
		public IGeneratorModule Generator { get; set; }
		#endregion

		#region Protected Properties
		protected virtual int CurrentListSize => this.MaximumListSize;

		protected int MaximumListSize { get; set; }

		protected virtual IList<TOutput> Pages { get; } = new List<TOutput>();
		#endregion

		#region Public Methods
		public PageSetResult<TOutput> CreatePageSet() =>
			new PageSetResult<TOutput>(this.Pages)
			{
				BadRevisionIds = new List<long>(this.badRevisionIds),
				Converted = this.converted.AsReadOnly(),
				Interwiki = this.interwiki.AsReadOnly(),
				Normalized = this.normalized.AsReadOnly(),
				Redirects = this.redirects.AsReadOnly(),
			};

		public PageSetResult<TOutput> SubmitPageSet(TInput input)
		{
			this.MaximumListSize = this.Wal.MaximumPageSetSize;
			this.values = new List<string>(input.Values ?? new string[0]);
			if (input.GeneratorInput != null)
			{
				this.Generator = this.Wal.ModuleFactory.CreateGenerator(input.GeneratorInput);
			}

			this.Wal.ClearWarnings();
			this.BeforeSubmit(input);
			this.ContinueModule = this.Wal.ModuleFactory.CreateContinue();
			this.ContinueModule.OnSubmit(this);
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
				var match = ParsingExtensions.TooManyFinder.Match(text);
				if (match.Success)
				{
					var parameter = match.Groups["parameter"].Value;
					if (PageSetInputBase.AllTypes.Contains(parameter))
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

		#region Protected Methods
		protected void DeserializeTitle(JToken result, TOutput page)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(page, nameof(page));
			page.Namespace = (int?)result["ns"];
			var pageId = (long?)result["pageid"]; // We want to keep the nullable version for Title checking
			page.PageId = pageId ?? 0;
			page.Title = (string)result["title"] ?? FakeTitleFromId(pageId);
			if (page.Title == null)
			{
				// In older versions of MediaWiki, some generators could return missing pages with no title or id, most commonly when links tables were out of date and needed refreshLinks.php run on them. If we get one of these, skip to the next page, there's nothing else we can do.
				return;
			}

			this.DeserializePage(result, page);
		}

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

			result["converted"].AddToDictionary(this.converted, "from", "to");
			var links = result["interwiki"].GetInterwikiLinks();
			foreach (var link in links)
			{
				this.interwiki.Add(link.Title, link);
			}

			result["normalized"].AddToDictionary(this.normalized, "from", "to");
			result["redirects"].GetRedirects(this.redirects);
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
		}

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			base.DeserializeParent(parent);

			// This is a fugly workaround for the fact that modules other than queries will have the pageset data at the parent level, while query has it at the child level. I couldn't figure out a better way to do it (other than to simply ignore it and check for results that will never be there).
			if (!(this is ActionQuery))
			{
				this.GetExceptions(parent);
			}
		}

		protected override IReadOnlyList<TOutput> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			foreach (var item in result)
			{
				this.DeserializeTitle(item, new TOutput());
			}

			// PageSets don't actually return a value here, instead returning it in Pages, so return null instead.
			return null;
		}
		#endregion

		#region Private Static Methods
		private static string FakeTitleFromId(long? pageId) => pageId == null ? null : "#" + pageId.Value.ToStringInvariant();
		#endregion
	}
}
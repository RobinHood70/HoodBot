#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.IO;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	public abstract class ActionModulePageSet<TInput, TOutput> : ActionModule, IPageSetGenerator
		where TInput : PageSetInput
		where TOutput : ITitle
	{
		#region Static Fields
		private static readonly Regex TooManyFinder = new Regex(@"Too many values .*?'(?<parameter>.*?)'.*?limit is (?<sizelimit>[0-9]+)", RegexOptions.Compiled, DefaultRegexTimeout);
		#endregion

		#region Fields
		private readonly HashSet<long> badRevisionIds = new HashSet<long>();
		private readonly Dictionary<string, string> converted = new Dictionary<string, string>(StringComparer.Ordinal);
		private readonly Dictionary<string, InterwikiTitleItem> interwiki = new Dictionary<string, InterwikiTitleItem>(StringComparer.Ordinal);
		private readonly Dictionary<string, string> normalized = new Dictionary<string, string>(StringComparer.Ordinal);
		private readonly Dictionary<string, PageSetRedirectItem> redirects = new Dictionary<string, PageSetRedirectItem>(StringComparer.Ordinal);
		private int offset;
		#endregion

		#region Constructors
		protected ActionModulePageSet(WikiAbstractionLayer wal)
			: base(wal)
		{
			this.ContinueModule = wal.ModuleFactory.CreateContinue();
			this.MaximumListSize = this.Wal.MaximumPageSetSize;
		}
		#endregion

		#region Public Properties
		public IGeneratorModule? Generator { get; protected set; }
		#endregion

		#region Protected Properties
		protected ContinueModule ContinueModule { get; set; }

		protected int MaximumListSize { get; set; }
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Virtual Properties
		protected virtual bool Continues => true;

		protected virtual int CurrentListSize => this.MaximumListSize;
		#endregion

		#region Public Methods
		public virtual PageSetResult<TOutput> Submit(TInput input)
		{
			ThrowNull(input, nameof(input));
			this.Wal.ClearWarnings();
			if (input.GeneratorInput != null)
			{
				this.Generator = this.Wal.ModuleFactory.CreateGenerator(input.GeneratorInput, this);
			}

			this.BeforeSubmit();
			this.ContinueModule.BeforePageSetSubmit(this);
			this.offset = 0;
			var pages = this.CreatePageList();

			do
			{
				do
				{
					var request = this.CreateRequest(input);
					var response = this.Wal.SendRequest(request);
					this.ParseResponse(response, pages);
				}
				while (this.ContinueModule.Continues && this.Continues);

				this.offset += this.CurrentListSize;
			}
			while (this.offset < input.Values.Count);

			return this.CreatePageSet(pages);
		}
		#endregion

		#region Protected Static Methods
		[return: NotNullIfNotNull("pageId")]
		protected static string? FakeTitleFromId(long? pageId) => pageId == null ? null : '#' + pageId.Value.ToStringInvariant();
		#endregion

		#region Protected Methods
		protected PageSetResult<TOutput> CreatePageSet(IList<TOutput> pages) => new PageSetResult<TOutput>(
			titles: pages,
			badRevisionIds: new List<long>(this.badRevisionIds),
			converted: this.converted,
			interwiki: this.interwiki,
			normalized: this.normalized,
			redirects: this.redirects);

		protected void GetPageSetNodes(JToken result)
		{
			ThrowNull(result, nameof(result));
			if (result["badrevids"] is JToken node)
			{
				foreach (var item in node)
				{
					// TODO: Was item.First?["revid"] - need to figure out if this was a simple error or version difference.
					if (item["revid"] is JToken revid)
					{
						this.badRevisionIds.Add((long?)revid ?? 0);
					}
				}
			}

			AddToDictionary(result["converted"], this.converted);
			var links = result["interwiki"].GetInterwikiLinks();
			foreach (var link in links)
			{
				this.interwiki.Add(link.Title, link);
			}

			AddToDictionary(result["normalized"], this.normalized);
			result["redirects"].GetRedirects(this.redirects, this.Wal.InterwikiPrefixes, this.SiteVersion);
		}

		protected void ParseResponse(string? response, IList<TOutput> pages)
		{
			var result = ToJson(response);
			if (result.Type == JTokenType.Object)
			{
				this.DeserializeAction(result);
				if (result[this.Name] is JToken node && node.Type != JTokenType.Null)
				{
					this.DeserializeResult(node, pages);
				}
				else
				{
					throw WikiException.General("no-result", "The expected result node, " + this.Name + ", was not found.");
				}
			}
			else if (!(result is JArray array && array.Count == 0))
			{
				throw new InvalidDataException();
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void BuildRequestPageSet(Request request, TInput input);

		protected abstract TOutput GetItem(JToken result);
		#endregion

		#region Protected Override Methods
		protected override void DeserializeActionExtra(JToken result)
		{
			ThrowNull(result, nameof(result));
			if (this.ContinueModule != null)
			{
				this.ContinueModule = this.ContinueModule.Deserialize(this.Wal, result);
			}

			this.GetPageSetNodes(result);
		}

		protected override bool HandleWarning(string from, string text)
		{
			if (string.Equals(from, this.Name, StringComparison.Ordinal) &&
				TooManyFinder.Match(text) is var match &&
				match.Success &&
				PageSetInput.AllTypes.Contains(match.Groups["parameter"].Value))
			{
				this.MaximumListSize = int.Parse(match.Groups["sizelimit"].Value, CultureInfo.InvariantCulture);
				return true;
			}

			return base.HandleWarning(from, text);
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual IList<TOutput> CreatePageList() => new List<TOutput>();

		protected virtual void DeserializeResult(JToken result, IList<TOutput> pages)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(pages, nameof(pages));
			foreach (var item in result)
			{
				pages.Add(this.GetItem(item));
			}
		}
		#endregion

		#region Private Static Methods
		private static void AddToDictionary(JToken? token, IDictionary<string, string> dict)
		{
			if (token != null)
			{
				foreach (var item in token)
				{
					dict.Add(item.MustHaveString("from"), item.MustHaveString("to"));
				}
			}
		}
		#endregion

		#region Private Methods
		private Request CreateRequest(TInput input)
		{
			ThrowNull(input, nameof(input));
			var request = this.CreateBaseRequest();
			request.Prefix = this.Prefix;
			if (this.Generator != null)
			{
				request.Add("generator", this.Generator.Name);
				this.Generator.BuildRequest(request);
			}

			if (input.Values != null && input.Values.Count > 0)
			{
				var numRemaining = input.Values.Count - this.offset;
				var listSize = numRemaining < this.CurrentListSize
					? numRemaining
					: this.CurrentListSize;
				Debug.Assert(listSize >= 0, "listSize was 0 or negative!");
				var currentGroup = new List<string>(listSize);
				for (var i = 0; i < listSize; i++)
				{
					currentGroup.Add(input.Values[this.offset + i]);
				}

				request.Add(input.TypeName, currentGroup);
			}

			request
				.AddIf("converttitles", input.ConvertTitles, input.GeneratorInput != null || input.ListType == ListType.Titles)
				.AddIf("redirects", input.Redirects, input.ListType != ListType.RevisionIds);

			this.BuildRequestPageSet(request, input);

			request.Prefix = string.Empty;
			this.ContinueModule?.BuildRequest(request);

			return request;
		}
		#endregion
	}
}
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.IO;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	public abstract class ActionModulePageSet<TInput, TOutput> : ActionModule, IPageSetGenerator
		where TInput : PageSetInput
		where TOutput : class, IApiTitle
	{
		#region Static Fields
		private static readonly Regex TooManyFinder = new("Too many values .*?['\"](?<parameter>.*?)['\"].*?limit is (?<sizelimit>[0-9]+)", RegexOptions.Compiled, Globals.DefaultRegexTimeout);
		#endregion

		#region Fields
		private readonly HashSet<long> badRevisionIds = new();
		private readonly Dictionary<string, string> converted = new(StringComparer.Ordinal);
		private readonly Dictionary<string, InterwikiTitleItem> interwiki = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string> normalized = new(StringComparer.Ordinal);
		private readonly Dictionary<string, PageSetRedirectItem> redirects = new(StringComparer.Ordinal);
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
			this.Wal.ClearWarnings();
			input.ThrowNull();
			if (input.GeneratorInput is IGeneratorInput genInput)
			{
				this.Generator = this.Wal.ModuleFactory.CreateGenerator(genInput, this);
			}

			HashSet<string> uniqueTitles = new(input.Values, StringComparer.Ordinal);
			this.BeforeSubmit();
			this.ContinueModule.BeforePageSetSubmit(this);
			var pages = this.CreatePageList();
			var lastRequest = string.Empty;

			int uniqueTitlesCount;
			do
			{
				uniqueTitlesCount = uniqueTitles.Count;
				do
				{
					var numRemaining = uniqueTitles.Count;
					var listSize = numRemaining < this.CurrentListSize
						? numRemaining
						: this.CurrentListSize;
					var currentGroup = new string[listSize];
					uniqueTitles.CopyTo(currentGroup, 0, listSize);

					var request = this.CreateRequest(input, currentGroup);
					var requestText = RequestVisitorUrl.Build(request);
					if (requestText.OrdinalEquals(lastRequest))
					{
						throw new InvalidOperationException("Infinite query loop detected.");
					}

					var response = this.Wal.SendRequest(request);
					this.ParseResponse(response, pages);
					if (input.ListType != ListType.Titles)
					{
						// This is a kludge to deal with the fact that there's often no clear identifier between input and results. For example, when purging by Page ID, no Page ID is ever returned, so we have to take it on faith that what was requested was purged.
						uniqueTitles.ExceptWith(currentGroup);
					}
				}
				while (this.ContinueModule.Continues && this.Continues);

				// Because pages may have returned less than we asked for (e.g., due to limits being surpassed), we remove all the pages we got back from our input set and continue from there.
				List<string> returnedNames = new();
				foreach (var title in pages)
				{
					returnedNames.Add(title.FullPageName);
				}

				if (input.ListType == ListType.Titles)
				{
					uniqueTitles.ExceptWith(returnedNames);
					uniqueTitles.ExceptWith(this.converted.Keys);
					uniqueTitles.ExceptWith(this.redirects.Keys);
				}
			}
			while (uniqueTitles.Count > 0 && uniqueTitlesCount != uniqueTitles.Count);
			return this.CreatePageSet(pages);
		}
		#endregion

		#region Protected Static Methods
		[return: NotNullIfNotNull(nameof(pageId))]
		protected static string? FakeTitleFromId(long? pageId) => pageId == null ? null : '#' + pageId.Value.ToStringInvariant();
		#endregion

		#region Protected Methods
		protected PageSetResult<TOutput> CreatePageSet(IList<TOutput> pages) => new(
			titles: pages,
			badRevisionIds: new List<long>(this.badRevisionIds),
			converted: this.converted,
			interwiki: this.interwiki,
			normalized: this.normalized,
			redirects: this.redirects);

		protected void GetPageSetNodes(JToken result)
		{
			result.ThrowNull();
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

		/// <summary>Parses the response.</summary>
		/// <param name="response">The response.</param>
		/// <param name="pages">The pages to parse.</param>
		/// <exception cref="InvalidDataException">Thrown when the result data isn't valid Json or is anything other than an empty array.</exception>
		/// <exception cref="WikiException">Thrown when the Json data is valid but the expected result tag could not be found.</exception>
		protected void ParseResponse(string? response, IList<TOutput> pages)
		{
			try
			{
				var result = ToJson(response.NotNull());
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
				else if (result is not JArray array || array.Count != 0)
				{
					throw new InvalidDataException();
				}
			}
			catch (JsonReaderException jre)
			{
				throw new InvalidDataException(EveMessages.ResultInvalid, jre);
			}
			catch (WikiException we) when (
				we.Code?.StartsWith("too-many-", StringComparison.Ordinal) == true &&
				TooManyFinder.Match(we.Info ?? string.Empty) is var match &&
				match.Success &&
				PageSetInput.AllTypes.Contains(match.Groups["parameter"].Value, StringComparer.Ordinal))
			{
				// TODO: This still counts 50 in the outer loop, which it shouldn't. See if we can figure out a way to report actual result size.
				this.MaximumListSize = int.Parse(match.Groups["sizelimit"].Value, CultureInfo.InvariantCulture);
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
			result.ThrowNull();
			if (this.ContinueModule != null)
			{
				this.ContinueModule = this.ContinueModule.Deserialize(this.Wal, result);
			}

			this.GetPageSetNodes(result);
		}

		protected override bool HandleWarning(string from, string text)
		{
			text.ThrowNull();
			if (string.Equals(from, this.Name, StringComparison.Ordinal) &&
				TooManyFinder.Match(text) is var match &&
				match.Success &&
				PageSetInput.AllTypes.Contains(match.Groups["parameter"].Value, StringComparer.Ordinal))
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
			pages.ThrowNull();
			foreach (var item in result.NotNull())
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
					dict[item.MustHaveString("from")] = item.MustHaveString("to");
				}
			}
		}
		#endregion

		#region Private Methods
		private Request CreateRequest(TInput input, IEnumerable<string> currentGroup)
		{
			input.ThrowNull();
			var request = this.CreateBaseRequest();
			request.Prefix = this.Prefix;
			if (this.Generator != null)
			{
				request.Add("generator", this.Generator.Name);
				this.Generator.BuildRequest(request);
			}

			request.Add(input.TypeName, currentGroup);
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
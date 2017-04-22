namespace WallE.Implementations.Eve
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using Design;
	using Http;
	using Newtonsoft.Json.Linq;
	using Pages;
	using static Design.Extensions;
	using static Globals;

	public class PageSet : QueryModule<PageSetInput, PagesOutput<PageOutput>>, IPageSet
	{
		#region Fields
		private KeyedPages currentPages = new KeyedPages();
		private IEnumerable<string> currentGroup;
		private string parentName;
		#endregion

		#region Constructors
		[SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Validated by alternate constructor")]
		public PageSet(WikiAbstractionLayer wal, PageSetInput pageSetInput, IEnumerable<IPropertyInput> pageProperties)
			: base(wal, pageSetInput, new PagesOutput<PageOutput>())
		{
			ThrowNull(wal, nameof(wal));
			ThrowNull(pageSetInput, nameof(pageSetInput));
			this.Modules = wal.ModuleFactory.InstantiateMany(pageProperties);
			this.Values = pageSetInput.Values;
			this.parentName = "query";
		}
		#endregion

		#region Public Properties
		public bool Done { get; private set; }

		public HashSet<string> DisabledModules { get; } = new HashSet<string>();

		public IGeneratorModule Generator { get; protected set; }

		public int MaximumListSize { get; set; }

		public ModuleCollection<IQueryModule> Modules { get; }

		public int Offset { get; set; }

		public IEnumerable<string> Values { get; }
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "pages";

		// This obviously cannot be a generator in its own right, so short-circuit the generator check that's normally in the query Prefix.
		public override string Prefix { get; } = string.Empty;
		#endregion

		#region Protected Override Properties
		protected override string BasePrefix { get; } = string.Empty;

		protected override string Type { get; } = null;
		#endregion

		#region Public Static Methods
		public ReadOnlyDictionary<string, string> GetConvertedTitles(JToken parent)
		{
			if (parent != null)
			{
				var titles = new Dictionary<string, string>(this.Output.Converted);
				parent.AddToDictionary(titles, "from", "to");

				return titles.AsReadOnly();
			}

			return EmptyReadOnlyDictionary<string, string>();
		}

		public ReadOnlyCollection<InterwikiTitle> GetInterwikiTitles(JToken parent)
		{
			if (parent != null)
			{
				var interwikiTitles = new HashSet<InterwikiTitle>(this.Output.Interwiki);
				foreach (var node in parent)
				{
					var title = new InterwikiTitle();
					title.InterwikiPrefix = (string)node["iw"];
					title.Title = (string)node["title"];
					title.Url = (Uri)node["url"];

					interwikiTitles.Add(title);
				}

				return interwikiTitles.AsReadOnly();
			}

			return EmptyReadOnlyCollection<InterwikiTitle>();
		}

		public ReadOnlyCollection<long> GetMissingRevisionIds(JToken parent)
		{
			if (parent != null)
			{
				var badRevisionIds = new HashSet<long>(this.Output.BadRevisionIds);
				foreach (JProperty node in parent)
				{
					badRevisionIds.Add((long)node.First["revid"]);
				}

				return badRevisionIds.AsReadOnly();
			}

			return EmptyReadOnlyCollection<long>();
		}

		public ReadOnlyDictionary<string, string> GetNormalizedTitles(JToken parent)
		{
			if (parent != null)
			{
				var titles = new Dictionary<string, string>(this.Output.Normalized);
				parent.AddToDictionary(titles, "from", "to");
				return titles.AsReadOnly();
			}

			return EmptyReadOnlyDictionary<string, string>();
		}

		public ReadOnlyDictionary<string, PageSetRedirect> GetRedirectTitles(JToken redirectsNode)
		{
			if (redirectsNode != null)
			{
				var titles = new Dictionary<string, PageSetRedirect>(this.Output.Redirects);
				foreach (var node in redirectsNode)
				{
					var toPage = new PageSetRedirect();
					var from = (string)node["from"];
					toPage.Title = (string)node["to"];
					toPage.Fragment = (string)node["tofragment"];
					toPage.Interwiki = (string)node["tointerwiki"];

					var gi = node.ToObject<Dictionary<string, object>>();
					gi.Remove("from");
					gi.Remove("to");
					gi.Remove("tofragment");
					gi.Remove("tointerwiki");

					toPage.GeneratorInfo = gi.AsReadOnly();

					titles.Add(from, toPage);
				}

				return titles.AsReadOnly();
			}

			return EmptyReadOnlyDictionary<string, PageSetRedirect>();
		}
		#endregion

		#region Public Methods
		public PageOutput PageFactory(string pageName) => this.Wal.PageOutputFactory(pageName);

		public void SetCurrentGroup(IEnumerable<string> group)
		{
			this.currentGroup = group;
			this.currentPages = new KeyedPages();
		}
		#endregion

		#region Public Override Methods
		public override bool HandleWarning(string from, string line)
		{
			if (from == this.parentName)
			{
				var match = Parsing.TooManyFinder.Match(line);
				if (match.Success)
				{
					var parameter = match.Groups["parameter"].Value;
					if (PageSetInputBase.AllTypes.Contains(parameter))
					{
						this.MaximumListSize = int.Parse(match.Groups["sizelimit"].Value, CultureInfo.InvariantCulture);
						this.Offset = this.MaximumListSize;
						return true;
					}
				}
			}

			return base.HandleWarning(from, line);
		}

		public void GetOutput(PagesOutputBase output)
		{
			throw new NotImplementedException();
		}

		public void DeserializeParent(JToken parent)
		{
			throw new NotImplementedException();
		}

		public void AddWarning(string from, string line)
		{
			throw new NotImplementedException();
		}

		bool IPageSet.HandleWarning(string from, string line)
		{
			throw new NotImplementedException();
		}

		public void DeserializePage(JToken result, PageSetPage item)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(PhpRequest request, PageSetInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Prefix = string.Empty;
			if (input.GeneratorInput != null)
			{
				var generator = this.Wal.ModuleFactory.Instantiate(input.GeneratorInput);
				generator.IsGenerator = true;
				request.Add("generator", generator.Name);
				generator.BuildRequest(request);
				this.Generator = (IGeneratorModule)generator;
			}
			else
			{
				request.AddPiped(input.TypeName, this.currentGroup);
			}

			request.AddBooleanIf("converttitles", input.ConvertTitles, input.GeneratorInput != null || input.ListType == ListType.Titles);
			request.AddBooleanIf("redirects", input.Redirects, input.ListType != ListType.RevisionIds);
			foreach (var module in this.Modules)
			{
				if (!this.DisabledModules.Contains(module.Name))
				{
					module.BuildRequest(request);
				}
			}
		}

		protected override void DeserializeParent(JToken parent, PagesOutput<PageOutput> output)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(output, nameof(output));

			// Sections are current hard-coded. Do we need to make these parameters?
			output.BadRevisionIds = this.GetMissingRevisionIds(parent["badrevids"]);
			output.Converted = this.GetConvertedTitles(parent["converted"]);
			output.Interwiki = this.GetInterwikiTitles(parent["interwiki"]);
			output.Normalized = this.GetNormalizedTitles(parent["normalized"]);
			output.Redirects = this.GetRedirectTitles(parent["redirects"]);
		}

		protected override void DeserializeResult(JToken result, PagesOutput<PageOutput> output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));
			foreach (var page in result)
			{
				this.GetPage(page);
			}

			var pages = new List<PageOutput>(this.Output.Pages);
			pages.AddRange(this.currentPages);
			output.Pages = pages.AsReadOnly();
		}
		#endregion

		#region Private Methods
		private static string FakeTitleFromId(long? pageId) => pageId == null ? null : "#" + ((long)pageId).ToStringInvariant();

		private void GetPage(JToken result)
		{
			var innerResult = this.Wal.DetectedFormatVersion == 2 ? result : result?.First;
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

			PageOutput item;
			if (this.currentPages.TryGetItem(search, out item))
			{
				this.LoopCount = 0;
			}
			else
			{
				item = this.PageFactory(search);
				item.WikiNamespace = (int?)innerResult["ns"];
				item.PageId = (long?)innerResult["pageid"] ?? 0;
				item.Missing = innerResult["missing"].AsBool();
				item.Invalid = innerResult["invalid"].AsBool();
				item.StartTimestamp = item.StartTimestamp ?? this.Wal.CurrentTimestamp;
				this.currentPages.Add(item);
				this.LoopCount = 1;
			}

			foreach (var module in this.Modules)
			{
				var pageSetter = module as IPageOutputSetter;
				if (pageSetter == null)
				{
					throw new InvalidCastException();
				}

				pageSetter.SetPageOutput(item);
				module.Deserialize(innerResult);
			}
		}
		#endregion

		#region Private Classes
		private class KeyedPages : KeyedCollection<string, PageOutput>
		{
			public bool TryGetItem(string pageTitle, out PageOutput item)
			{
				item = null;
				return this.Dictionary?.TryGetValue(pageTitle, out item) ?? false;
			}

			protected override string GetKeyForItem(PageOutput item) => item?.Title;
		}
		#endregion
	}
}
namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using Design;
	using WallE;
	using WallE.Base;
	using static Globals;

	/// <summary>A collection of Title objects.</summary>
	/// <remarks>This collection class functions similar to a KeyedCollection, but automatically overwrites existing items with new ones. Because Title objects don't support changing item keys, neither does this.</remarks>
	public class TitleCollection : TitleCollectionBase<Title>
	{
		#region Constructors
		public TitleCollection(Site site)
			: base(site)
		{
		}

		public TitleCollection(Site site, IEnumerable<string> titles)
			: base(site)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var item in titles)
			{
				var newTitle = new Title(site, item);
				this.Add(newTitle);
			}
		}

		public TitleCollection(Site site, params string[] titles)
			: this(site, titles as IEnumerable<string>)
		{
		}

		public TitleCollection(Site site, int ns, IEnumerable<string> titles)
			: base(site)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var item in titles)
			{
				var newTitle = Title.ForcedNamespace(site, ns, item);
				this.Add(newTitle);
			}
		}

		public TitleCollection(Site site, int ns, params string[] titles)
			: this(site, ns, titles as IEnumerable<string>)
		{
		}
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class from individual Title items.</summary>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection.</returns>
		/// <remarks>This constructor performs a deep copy of the original collection.</remarks>
		public static TitleCollection CopyFrom(params Title[] titles) => CopyFrom(titles as IEnumerable<Title>);

		/// <summary>Initializes a new instance of the <see cref="TitleCollection"/> class from another Title collection.</summary>
		/// <param name="titles">The original Title collection.</param>
		/// <returns>A Title-only copy of the original collection.</returns>
		/// <remarks>This constructor performs a deep copy of the original collection.</remarks>
		public static TitleCollection CopyFrom(IEnumerable<Title> titles)
		{
			ThrowNull(titles, nameof(titles));
			Site site = null;
			foreach (var title in titles)
			{
				site = site ?? title.Site;
				break;
			}

			if (site == null)
			{
				throw new InvalidOperationException("Source collection is empty - TitleCollection could not be initialized.");
			}

			var output = new TitleCollection(site);
			foreach (var title in titles)
			{
				var newTitle = new Title(title);
				output.Add(newTitle);
			}

			return output;
		}
		#endregion

		#region Public Methods
		public void Add(params string[] titles) => this.Add(titles as IEnumerable<string>);

		public void Add(IEnumerable<string> titles)
		{
			ThrowNull(titles, nameof(titles));
			foreach (var title in titles)
			{
				this.Add(new Title(this.Site, title));
			}
		}

		public void Add(IEnumerable<IWikiTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.Add(new Title(title));
				}
			}
		}

		public void AddCategories() => this.AddCategories(null);

		public void AddCategories(string prefix)
		{
			var input = new AllCategoriesInput()
			{
				Prefix = prefix,
				Properties = AllCategoriesProperties.Hidden,
			};
			var result = this.Site.AbstractionLayer.AllCategories(input);
			this.FillFromTitleItems(result);
		}

		public void AddCategories(string from, string to)
		{
			var input = new AllCategoriesInput()
			{
				From = from,
				To = to,
				Properties = AllCategoriesProperties.Hidden,
			};
			var result = this.Site.AbstractionLayer.AllCategories(input);
			this.FillFromTitleItems(result);
		}

		public void AddFiles(string user)
		{
			var input = new AllImagesInput
			{
				User = user,
			};
			var result = this.Site.AbstractionLayer.AllImages(input);
			this.FillFromTitleItems(result);
		}

		public void AddFiles(string from, string to)
		{
			var input = new AllImagesInput
			{
				From = from,
				To = to,
			};
			var result = this.Site.AbstractionLayer.AllImages(input);
			this.FillFromTitleItems(result);
		}

		public void AddFiles(DateTime? start, DateTime? end)
		{
			var input = new AllImagesInput
			{
				Start = start,
				End = end,
			};
			var result = this.Site.AbstractionLayer.AllImages(input);
			this.FillFromTitleItems(result);
		}

		public void AddFileUsage()
		{
			var input = new AllFileUsagesInput();
			var result = this.Site.AbstractionLayer.AllFileUsages(input);
			this.FillFromTitleItems(result);
		}

		public void AddFileUsage(string prefix)
		{
			var input = new AllFileUsagesInput() { Prefix = prefix };
			var result = this.Site.AbstractionLayer.AllFileUsages(input);
			this.FillFromTitleItems(result);
		}

		public void AddFileUsage(string from, string to)
		{
			var input = new AllFileUsagesInput() { From = from, To = to };
			var result = this.Site.AbstractionLayer.AllFileUsages(input);
			this.FillFromTitleItems(result);
		}

		public void AddLinksToNamespace(int ns)
		{
			var input = new AllLinksInput() { Namespace = ns };
			var result = this.Site.AbstractionLayer.AllLinks(input);
			this.FillFromTitleItems(result);
		}

		public void AddLinksToNamespace(int ns, string prefix)
		{
			var input = new AllLinksInput() { Namespace = ns, Prefix = prefix };
			var result = this.Site.AbstractionLayer.AllLinks(input);
			this.FillFromTitleItems(result);
		}

		public void AddLinksToNamespace(int ns, string from, string to)
		{
			var input = new AllLinksInput() { Namespace = ns, From = from, To = to };
			var result = this.Site.AbstractionLayer.AllLinks(input);
			this.FillFromTitleItems(result);
		}

		public void AddMessages(Filter modifiedMessages)
		{
			var input = new AllMessagesInput() { FilterModified = (FilterOption)modifiedMessages };
			var result = this.Site.AbstractionLayer.AllMessages(input);
			this.FillFromTitleItems(result);
		}

		public void AddMessages(Filter modifiedMessages, IEnumerable<string> messages)
		{
			var input = new AllMessagesInput() { FilterModified = (FilterOption)modifiedMessages, Messages = messages };
			var result = this.Site.AbstractionLayer.AllMessages(input);
			this.FillFromTitleItems(result);
		}

		public void AddMessages(Filter modifiedMessages, string prefix)
		{
			var input = new AllMessagesInput() { FilterModified = (FilterOption)modifiedMessages, Prefix = prefix };
			var result = this.Site.AbstractionLayer.AllMessages(input);
			this.FillFromTitleItems(result);
		}

		public void AddMessages(Filter modifiedMessages, string from, string to)
		{
			var input = new AllMessagesInput() { FilterModified = (FilterOption)modifiedMessages, MessageFrom = from, MessageTo = to };
			var result = this.Site.AbstractionLayer.AllMessages(input);
			this.FillFromTitleItems(result);
		}

		public void AddNamespace(int ns, Filter redirects) => this.AddNamespace(ns, redirects, null, null);

		public void AddNamespace(int ns, Filter redirects, string startsWith)
		{
			var input = new AllPagesInput()
			{
				FilterRedirects = (FilterOption)redirects,
				Namespace = ns,
				Prefix = startsWith,
			};
			this.FillFromTitleItems(this.Site.AbstractionLayer.AllPages(input));
		}

		public void AddNamespace(int ns, Filter redirects, string fromPage, string toPage)
		{
			var input = new AllPagesInput()
			{
				FilterRedirects = (FilterOption)redirects,
				From = fromPage,
				Namespace = ns,
				To = toPage,
			};
			var result = this.Site.AbstractionLayer.AllPages(input);
			this.FillFromTitleItems(result);
		}

		public void AddRedirectsToNamespace(int ns)
		{
			var input = new AllRedirectsInput() { Namespace = ns };
			var result = this.Site.AbstractionLayer.AllRedirects(input);
			this.FillFromTitleItems(result);
		}

		public void AddRedirectsToNamespace(int ns, string prefix)
		{
			var input = new AllRedirectsInput() { Namespace = ns, Prefix = prefix };
			var result = this.Site.AbstractionLayer.AllRedirects(input);
			this.FillFromTitleItems(result);
		}

		public void AddRedirectsToNamespace(int ns, string from, string to)
		{
			var input = new AllRedirectsInput() { Namespace = ns, From = from, To = to };
			var result = this.Site.AbstractionLayer.AllRedirects(input);
			this.FillFromTitleItems(result);
		}

		public void AddRevisions(DateTime? start, DateTime? end)
		{
			var input = new AllRevisionsInput() { Start = start, End = end };
			var result = this.Site.AbstractionLayer.AllRevisions(input);
			this.FillFromTitleItems(result);
		}

		public void AddRevisions(DateTime start, bool newer) => this.AddRevisions(start, newer, 0);

		public void AddRevisions(DateTime start, bool newer, int count)
		{
			var input = new AllRevisionsInput { Start = start, SortAscending = newer, MaxItems = count };
			var result = this.Site.AbstractionLayer.AllRevisions(input);
			this.FillFromTitleItems(result);
		}

		public void AddTemplateTransclusions()
		{
			var input = new AllTransclusionsInput();
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		public void AddTemplateTransclusions(string prefix)
		{
			var input = new AllTransclusionsInput() { Prefix = prefix };
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		public void AddTemplateTransclusions(string from, string to)
		{
			var input = new AllTransclusionsInput() { From = from, To = to };
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		public void AddTransclusionsOfNamespace(int ns)
		{
			var input = new AllTransclusionsInput() { Namespace = ns };
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		public void AddTransclusionsOfNamespace(int ns, string prefix)
		{
			var input = new AllTransclusionsInput() { Namespace = ns, Prefix = prefix };
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}

		public void AddTransclusionsOfNamespace(int ns, string from, string to)
		{
			var input = new AllTransclusionsInput() { Namespace = ns, From = from, To = to };
			var result = this.Site.AbstractionLayer.AllTransclusions(input);
			this.FillFromTitleItems(result);
		}
		#endregion

		#region Private Methods
		private void FillFromTitleItems(IEnumerable<ITitleOnly> result)
		{
			foreach (var item in result)
			{
				this.Add(new Title(this.Site, item.Title));
			}
		}
		#endregion
	}
}
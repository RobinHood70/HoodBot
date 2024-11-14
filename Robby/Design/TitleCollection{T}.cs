namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WikiCommon;

	/// <summary>Provides a base class to manipulate a collection of titles.</summary>
	/// <typeparam name="T">The type of the title.</typeparam>
	/// <seealso cref="IList{TTitle}" />
	/// <seealso cref="IReadOnlyCollection{TTitle}" />
	/// <remarks>This collection class functions similarly to a KeyedCollection. Unlike a KeyedCollection, however, new items will automatically overwrite previous ones rather than throwing an error. TitleCollection also does not support changing an item's key. You must use Remove/Add in combination.</remarks>
	public class TitleCollection<T> : KeyedCollection<Title, T>, IEnumerable<T>
		where T : ITitle
	{
		#region Public Constructors

		/// <summary>Initializes a new instance of the <see cref="TitleCollection{T}"/> class.</summary>
		/// <param name="site">The <see cref="Site"/> the titles in this collection belong to.</param>
		public TitleCollection(Site site)
		{
			ArgumentNullException.ThrowIfNull(site);
			this.Site = site;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether <see cref="NamespaceLimitations"/> specifies namespaces to be removed from the collection or only allowing those namepaces.</summary>
		/// <value>The type of the namespace limitation.</value>
		/// <remarks>Changing this property only affects newly added pages and does not affect any existing items in the collection. Use <see cref="FilterByLimitationRules"/> to do so, if needed.</remarks>
		public LimitationType LimitationType { get; set; } = LimitationType.Disallow;

		/// <summary>Gets the namespace limitations.</summary>
		/// <value>A set of namespace IDs that will be filtered out or filtered down to automatically as pages are added.</value>
		/// <remarks>Changing the contents of this collection only affects newly added pages and does not affect any existing items in the collection. Use <see cref="FilterByLimitationRules"/> to do so, if needed.</remarks>
		public ICollection<int> NamespaceLimitations { get; } =
		[
			MediaWikiNamespaces.Media,
			MediaWikiNamespaces.MediaWiki,
			MediaWikiNamespaces.Special,
			MediaWikiNamespaces.Template,
			MediaWikiNamespaces.User,
		];

		/// <summary>Gets the site for the collection.</summary>
		/// <value>The site.</value>
		public Site Site { get; }
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="ITitle">IHasTitle</see> with the specified key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The <see cref="ITitle">IHasTitle</see>.</returns>
		/// <remarks>Like a <see cref="Dictionary{TKey, TValue}"/>, this indexer will add a new entry on set if the requested entry isn't found.</remarks>
		public T this[string key]
		{
			get
			{
				ArgumentNullException.ThrowIfNull(key);
				return this[TitleFactory.FromUnvalidated(this.Site, key)];
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Adds multiple titles to the <see cref="TitleCollection">collection</see> at once.</summary>
		/// <param name="titles">The titles to add.</param>
		/// <remarks>This method is for convenience only. Unlike the equivalent <see cref="List{T}" /> function, it simply calls <see cref="Collection{T}.Add(T)" /> repeatedly and provides no performance benefit.</remarks>
		public void AddRange(IEnumerable<T> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					this.TryAdd(title);
				}
			}
		}

		/// <summary>Determines whether the collection contains an item with the specified key.</summary>
		/// <param name="key">The key to search for.</param>
		/// <returns><see langword="true" /> if the collection contains an item with the specified key; otherwise, <see langword="true" />.</returns>
		public bool Contains(string key)
		{
			ArgumentNullException.ThrowIfNull(key);
			var title = TitleFactory.FromUnvalidated(this.Site, key);
			return this.Contains(title);
		}

		/// <summary>Reapplies the namespace limitations in <see cref="NamespaceLimitations"/> to the existing collection.</summary>
		public void FilterByLimitationRules()
		{
			if (this.LimitationType == LimitationType.Disallow)
			{
				this.RemoveNamespaces(this.NamespaceLimitations);
			}
			else if (this.LimitationType == LimitationType.OnlyAllow)
			{
				this.FilterToNamespaces(this.NamespaceLimitations);
			}
		}

		/// <summary>Filters the collection to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to filter to.</param>
		public void FilterToNamespaces(IEnumerable<int> namespaces)
		{
			HashSet<int> hash = new(namespaces);
			for (var i = this.Count - 1; i >= 0; i--)
			{
				if (!hash.Contains(this.GetKeyForItem(this[i]).Namespace.Id))
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Filters the collection to one or more namespaces.</summary>
		/// <param name="namespaces">The namespaces to filter to.</param>
		public void FilterToNamespaces(params int[] namespaces) => this.FilterToNamespaces(namespaces as IEnumerable<int>);

		/// <summary>Determines the index of a specific item in the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="key">The key of the item to locate in the <see cref="TitleCollection">collection</see>.</param>
		/// <returns>The index of the item with the specified <paramref name="key" /> if found in the list; otherwise, -1.</returns>
		public int IndexOf(string key)
		{
			ArgumentNullException.ThrowIfNull(key);
			var title = TitleFactory.FromUnvalidated(this.Site, key);
			return this.IndexOf(this[title]);
		}

		/// <summary>Removes the item with the specified key from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="key">The key of the item to remove from the <see cref="TitleCollection">collection</see>.</param>
		/// <returns><see langword="true" /> if and item with the specified <paramref name="key" /> was successfully removed from the <see cref="TitleCollection">collection</see>; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if an item with the specified <paramref name="key" /> is not found in the original <see cref="TitleCollection">collection</see>.</returns>
		public bool Remove(string key)
		{
			ArgumentNullException.ThrowIfNull(key);
			var title = TitleFactory.FromUnvalidated(this.Site, key);
			return this.Remove(title);
		}

		/// <summary>Removes a series of items from the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="titles">The titless to remove.</param>
		/// <returns><see langword="true" /> if any of the <paramref name="titles" /> were removed; otherwise, <see langword="false" />.</returns>
		public bool Remove(IEnumerable<Title> titles)
		{
			ArgumentNullException.ThrowIfNull(titles);
			var removed = false;
			foreach (var item in titles)
			{
				removed |= this.Remove(item);
			}

			return removed;
		}

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(IEnumerable<int> namespaces) => this.RemoveNamespaces(false, namespaces);

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(params int[] namespaces) => this.RemoveNamespaces(false, namespaces as IEnumerable<int>);

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="removeTalk">Whether to remove talk spaces along with <paramref name="namespaces"/>.</param>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(bool removeTalk, IEnumerable<int>? namespaces)
		{
			if (namespaces is not HashSet<int> hash)
			{
				hash = new(namespaces ?? []);
			}

			for (var i = this.Count - 1; i >= 0; i--)
			{
				var ns = this.GetKeyForItem(this[i]).Namespace;
				if ((removeTalk && ns.IsTalkSpace) || hash.Contains(ns.Id))
				{
					this.RemoveAt(i);
				}
			}
		}

		/// <summary>Removes one or more namespaces from the collection.</summary>
		/// <param name="removeTalk">Whether to remove talk spaces along with <paramref name="namespaces"/>.</param>
		/// <param name="namespaces">The namespaces to remove.</param>
		public void RemoveNamespaces(bool removeTalk, params int[] namespaces) => this.RemoveNamespaces(removeTalk, namespaces as IEnumerable<int>);

		/// <summary>Removes all talk spaces from the collection.</summary>
		public void RemoveTalkNamespaces() => this.RemoveNamespaces(true, null as IEnumerable<int>);

		/// <summary>Sets namespace limitations for the Load() methods.</summary>
		/// <param name="limitationType">Type of the limitation.</param>
		/// <param name="namespaceLimitations">The namespace limitations to apply to the PageCollection returned.</param>
		/// <remarks>Limitations apply only to the current collection; result collections will inherently be unfiltered to allow for cross-namespace redirection. Filtering can be added to result collections after they are returned.</remarks>
		public void SetLimitations(LimitationType limitationType, params int[] namespaceLimitations) => this.SetLimitations(limitationType, namespaceLimitations as IEnumerable<int>);

		/// <summary>Randomize the collection order.</summary>
		public void Shuffle()
		{
			var random = new Random();
			var list = (List<T>)this.Items;
			var num = list.Count - 1;
			if (num > 0)
			{
				while (num >= 0)
				{
					var num2 = random.Next(list.Count);
					var index = num2;
					var index2 = num;
					var value = list[num];
					var value2 = list[num2];
					list[index] = value;
					list[index2] = value2;
					num--;
				}
			}
		}

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> using the specified <see cref="Comparison{T}" />.</summary>
		/// <param name="comparison">The comparison.</param>
		public void Sort(Comparison<T> comparison)
		{
			ArgumentNullException.ThrowIfNull(comparison);
			var list = (List<T>)this.Items;
			list.Sort(comparison);
		}

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> using the specified <see cref="IComparer{T}" />.</summary>
		/// <param name="comparer">The comparer.</param>
		public void Sort(IComparer<T> comparer)
		{
			ArgumentNullException.ThrowIfNull(comparer);
			var list = (List<T>)this.Items;
			list.Sort(comparer);
		}

		/// <summary>Returns the Title values of each item in the collection.</summary>
		/// <returns>The Title values of each item in the collection.</returns>
		public IEnumerable<Title> Titles()
		{
			foreach (var item in this)
			{
				yield return item.Title;
			}
		}

		/// <summary>Convert a collection of ITitles to their full page names.</summary>
		/// <returns>An enumeration of the titles converted to their full page names.</returns>
		public IEnumerable<string> ToFullPageNames()
		{
			foreach (var title in this)
			{
				yield return title.Title.FullPageName();
			}
		}

		/// <summary>Attempts to add a title with the given name to the list, gracefully skipping the item if it's already present.</summary>
		/// <param name="item">The item to try to add.</param>
		/// <returns><see langword="true"/> if the item was added; otherwise, <see langword="false"/>.</returns>
		public bool TryAdd(T item)
		{
			if (this.Contains(this.GetKeyForItem(item)))
			{
				return false;
			}

			this.Add(item);
			return true;
		}

		/// <summary>Attempts to add titles with the given names to the list, gracefully skipping them if they're already present.</summary>
		/// <param name="items">The items to try to add.</param>
		/// <returns><see langword="true"/> if any items were added; otherwise, <see langword="false"/>.</returns>
		public bool TryAddRange(IEnumerable<T> items)
		{
			ArgumentNullException.ThrowIfNull(items);
			var retval = false;
			foreach (var item in items)
			{
				retval |= this.TryAdd(item);
			}

			return retval;
		}

		/// <summary>Returns the requested value, or null if not found.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public T? ValueOrDefault(ITitle key)
		{
			ArgumentNullException.ThrowIfNull(key);
			_ = this.TryGetValue(key, out var retval);
			return retval;
		}

		/// <summary>Returns the requested value, or null if not found.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The requested value, or null if not found.</returns>
		public T? ValueOrDefault(string key)
		{
			ArgumentNullException.ThrowIfNull(key);
			return this.ValueOrDefault(TitleFactory.FromUnvalidated(this.Site, key));
		}
		#endregion

		#region Public Abstract Methods

		/// <summary>Sorts the items in the <see cref="TitleCollection">collection</see> by namespace, then pagename.</summary>
		public void Sort()
		{
			var list = (List<T>)this.Items;
			list.Sort((x, y) => x.Title.CompareTo(y.Title));
		}
		#endregion

		#region Public Virtual Methods

		/// <summary>Sets the namespace limitations to new values, clearing out any previous limitations.</summary>
		/// <param name="limitationType">The type of namespace limitations to apply.</param>
		/// <param name="namespaceLimitations">The namespace limitations. If null, only the limitation type is applied; the namespace set will remain unchanged.</param>
		/// <remarks>If the <paramref name="namespaceLimitations"/> parameter is null, no changes will be made to either of the limitation properties. This allows current/default limitations to remain in place if needed.</remarks>
		public virtual void SetLimitations(LimitationType limitationType, IEnumerable<int>? namespaceLimitations)
		{
			this.LimitationType = limitationType;
			this.NamespaceLimitations.Clear();
			if (limitationType != LimitationType.None && namespaceLimitations != null)
			{
				this.NamespaceLimitations.AddRange(namespaceLimitations);
			}
		}

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
		public virtual bool TryGetValue(ITitle key, [MaybeNullWhen(false)] out T value)
		{
			ArgumentNullException.ThrowIfNull(key);
			return base.TryGetValue(key.Title, out value);
		}

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
		public virtual bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
		{
			ArgumentNullException.ThrowIfNull(key);
			var title = TitleFactory.FromUnvalidated(this.Site, key).ToTitle();
			return base.TryGetValue(title, out value);
		}
		#endregion

		#region Protected Methods

		/// <summary>Gets a value indicating whether the page title is within the collection's limitations.</summary>
		/// <param name="title">The title.</param>
		/// <returns><see langword="true"/> if the page is within the collection's limitations and can be added to it; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the <see cref="LimitationType"/> is not one of the recognized values.</exception>
		protected virtual bool IsTitleInLimits(T title) =>
			title != null &&
			this.LimitationType switch
			{
				LimitationType.None => true,
				LimitationType.Disallow => !this.NamespaceLimitations.Contains(title.Title.Namespace.Id),
				LimitationType.OnlyAllow => this.NamespaceLimitations.Contains(title.Title.Namespace.Id),
				_ => throw new ArgumentOutOfRangeException(Resources.InvalidLimitationType)
			};
		#endregion

		#region Protected Override Methods

		/// <inheritdoc/>
		protected override Title GetKeyForItem(T item) => item.Title;

		/// <summary>Inserts an item into the <see cref="TitleCollection">collection</see>.</summary>
		/// <param name="index">The index to insert at.</param>
		/// <param name="item">The item.</param>
		/// <exception cref="ArgumentException">An element with the same key already exists in the collection.</exception>
		/// <exception cref="ArgumentNullException">The item is null.</exception>
		/// <exception cref="InvalidOperationException">The item's site does not match the collection's site.</exception>
		/// <remarks>This method underlies all methods that insert pages into the collection, and can be overridden in derived classes.</remarks>
		protected override void InsertItem(int index, T item)
		{
			ArgumentNullException.ThrowIfNull(item);
			if (item.Title.Site != item.Title.Site)
			{
				throw new InvalidOperationException(Resources.SiteMismatch);
			}

			if (this.IsTitleInLimits(item))
			{
				base.InsertItem(index, item);
			}
		}
		#endregion
	}
}
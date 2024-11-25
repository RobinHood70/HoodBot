namespace RobinHood70.WikiCommon.Parser;

// TODO: This class still needs some work to make sure it's consistent and has all reasonably useful methods (and possibly removing any kruft).
// TODO: Make this observable in some fashion. Might mean using an embedded list rather than inheriting from List<T> directly.
// TODO: Make this fluent.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using RobinHood70.CommonCode;

/// <summary>  A delegate for the method required by the Replace method.</summary>
/// <param name="node">The node.</param>
public delegate IList<IWikiNode>? NodeReplacer(IWikiNode node);

/// <summary>A collection of <see cref="IWikiNode"/>s representing wiki text. Implemented as a linked list.</summary>
public class WikiNodeCollection : List<IWikiNode>
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="WikiNodeCollection"/> class.</summary>
	/// <param name="factory">The factory to use to create new nodes.</param>
	public WikiNodeCollection(IWikiNodeFactory factory)
	{
		ArgumentNullException.ThrowIfNull(factory);
		this.Factory = factory;
	}

	/// <summary>Initializes a new instance of the <see cref="WikiNodeCollection"/> class.</summary>
	/// <param name="factory">The factory to use to create new nodes.</param>
	/// <param name="nodes">The nodes to initialize the collection with.</param>
	public WikiNodeCollection(IWikiNodeFactory factory, params IWikiNode[] nodes)
		: this(factory, (IEnumerable<IWikiNode>)nodes)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="WikiNodeCollection"/> class.</summary>
	/// <param name="factory">The factory to use to create new nodes.</param>
	/// <param name="nodes">The nodes to initialize the collection with.</param>
	public WikiNodeCollection(IWikiNodeFactory factory, IEnumerable<IWikiNode> nodes)
		: base(nodes)
	{
		ArgumentNullException.ThrowIfNull(factory);
		this.Factory = factory;
	}
	#endregion

	#region Public Properties

	/// <summary>Gets the <see cref="IWikiNodeFactory"/> used to create new nodes.</summary>
	/// <value>The factory.</value>
	public IWikiNodeFactory Factory { get; }

	/// <summary>Gets the <see cref="IHeaderNode"/>s on the page.</summary>
	/// <value>The header nodes.</value>
	public IEnumerable<IHeaderNode> HeaderNodes => this.FindAll<IHeaderNode>();

	/// <summary>Gets the <see cref="ILinkNode"/>s on the page.</summary>
	/// <value>The header nodes.</value>
	public IEnumerable<ILinkNode> LinkNodes => this.FindAll<ILinkNode>();

	/// <summary>Gets the <see cref="ITemplateNode"/>s on the page.</summary>
	/// <value>The header nodes.</value>
	public IEnumerable<ITemplateNode> TemplateNodes => this.FindAll<ITemplateNode>();

	/// <summary>Gets the <see cref="ITextNode"/>s on the page.</summary>
	/// <value>The header nodes.</value>
	public IEnumerable<ITextNode> TextNodes => this.FindAll<ITextNode>();
	#endregion

	#region Public Methods

	/// <summary>Accepts a visitor to process the node.</summary>
	/// <param name="visitor">The visiting class.</param>
	public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

	/// <summary>Adds text to the end of the collection.</summary>
	/// <param name="text">The text.</param>
	/// <remarks>Adds text to the final node in the collection if it's an <see cref="ITextNode"/>; otherwise, creates a text node (via the factory) with the specified text and adds it to the collection.</remarks>
	public void AddText([Localizable(false)] string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (this.Count > 0 && this[^1] is ITextNode node)
			{
				node.Text += text;
			}
			else
			{
				this.Add(this.Factory.TextNode(text));
			}
		}
	}

	/// <summary>Parses the provided text to the best of its ability before adding it to the current <see cref="WikiNodeCollection"/>.</summary>
	/// <remarks>Note that this parses <em>only</em> the text provided, so passing incomplete text for a node will result in incorrect nodes being added. For example, using AddParsed("[[Hello") and AddParsed("|Goodbye]])" will result in different nodes than using AddParsed("[[Hello|Goodbye]]").</remarks>
	/// <param name="text">The text to be added.</param>
	public void AppendParsed([Localizable(false)] string text)
	{
		var newNodes = this.Factory.Parse(text);
		this.AddRange(newNodes);
	}

	/// <summary>Creates a shallow copy of the collection.</summary>
	/// <returns>A shallow copy of all nodes in the collection, along with the factory.</returns>
	public WikiNodeCollection Clone()
	{
		var nodes = new IWikiNode[this.Count];
		this.CopyTo(nodes);
		return new WikiNodeCollection(this.Factory, nodes);
	}

	/// <summary>Copies the surrounding whitespace from the current WikiNodeCollection to the provided value.</summary>
	/// <param name="value">The value to format.</param>
	/// <returns>The value with the same surrounding whitespace as the provided WikiNodeCollection.</returns>
	public string CopyFormatTo(string? value)
	{
		var (leading, trailing) = this.ToValue().GetSurroundingWhitespace();
		return leading + value + trailing;
	}

	/// <summary>Finds the first node of the specified type.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <returns>The first node found, or null if no nodes of that type are in the collection.</returns>
	[return: MaybeNull]
	public T Find<T>()
		where T : IWikiNode => this.FindAll<T>().FirstOrDefault();

	/// <summary>Finds a single node of the specified type that satisfies the condition.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
	/// <returns>The first node (or the last, for reverse searches) in the collection that is of the specified type and satisfies the condition.</returns>
	/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
	[return: MaybeNull]
	public T Find<T>(Predicate<T>? condition)
		where T : class, IWikiNode => this.Find(condition, false, false, 0);

	/// <summary>Finds a single node of the specified type that satisfies the condition.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
	/// <param name="reverse">Set to <see langword="true"/> to reverse the search direction.</param>
	/// <returns>The first node (or the last, for reverse searches) in the collection that is of the specified type and satisfies the condition.</returns>
	/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
	[return: MaybeNull]
	public T Find<T>(Predicate<T>? condition, bool reverse)
		where T : class, IWikiNode => this.Find(condition, reverse, false, reverse ? this.Count - 1 : 0);

	/// <summary>Finds a single node of the specified type that satisfies the condition.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
	/// <param name="reverse">Set to <see langword="true"/> to reverse the search direction.</param>
	/// <param name="recursive">Set to <see langword="true"/> to search all nodes recursively; otherwise, only the top level of nodes will be searched.</param>
	/// <param name="startAt">The node to start searching at (inclusive). May be <see langword="null"/>.</param>
	/// <returns>The first node (or the last, for reverse searches) in the collection that is of the specified type and satisfies the condition.</returns>
	/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
	[return: MaybeNull]
	public T Find<T>(Predicate<T>? condition, bool reverse, bool recursive, int startAt)
		where T : class, IWikiNode =>
		this.FindAll(condition, reverse, recursive, startAt).FirstOrDefault();

	/// <summary>Finds all <see cref="LinkedListNode{T}"/>s with a <see cref="LinkedListNode{T}.Value">Value</see> of the specified type.</summary>
	/// <typeparam name="T">The type of node to find.</typeparam>
	/// <returns>The nodes in the collection that are of the specified type.</returns>
	/// <remarks>Outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned before the <c>{{Example|example}}</c> template.</remarks>
	public IEnumerable<T> FindAll<T>()
		where T : IWikiNode => this.FindAll<T>(null, false, true, 0);

	/// <summary>Finds all <see cref="LinkedListNode{T}"/>s with a <see cref="LinkedListNode{T}.Value">Value</see> of the specified type.</summary>
	/// <typeparam name="T">The type of node to find.</typeparam>
	/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
	/// <returns>The nodes in the collection that are of the specified type.</returns>
	/// <remarks>Outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned before the <c>{{Example|example}}</c> template.</remarks>
	public IEnumerable<T> FindAll<T>(Predicate<T>? condition)
		where T : IWikiNode => this.FindAll(condition, false, true, 0);

	/// <summary>Finds all <see cref="LinkedListNode{T}"/>s with a <see cref="LinkedListNode{T}.Value">Value</see> of the specified type that satisfy the condition.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
	/// <param name="reverse">Set to <see langword="true"/> to reverse the search direction.</param>
	/// <param name="recursive">Set to <see langword="true"/> to search all nodes recursively; otherwise, only the top level of nodes will be searched.</param>
	/// <param name="index">The node to start searching at (inclusive). May be <see langword="null"/>.</param>
	/// <returns>The first node (or the last, for reverse searches) in the collection that is of the specified type and satisfies the condition.</returns>
	/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
	public IEnumerable<T> FindAll<T>(Predicate<T>? condition, bool reverse, bool recursive, int index)
		where T : IWikiNode
	{
		if (this.Count == 0)
		{
			yield break;
		}

		var increment = reverse ? -1 : 1;
		var pastEnd = reverse ? -1 : this.Count;
		while (index != pastEnd)
		{
			if (this[index] is T typedNode && (condition == null || condition(typedNode)))
			{
				yield return typedNode;
			}

			if (recursive && this[index] is IParentNode parent)
			{
				foreach (var childCollection in parent.NodeCollections)
				{
					foreach (var value in childCollection.FindAll(condition, reverse, recursive, 0))
					{
						yield return value;
					}
				}
			}

			index += increment;
		}
	}

	/// <summary>Finds the index of the first node of type T that satisfies the predicate.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="match">The predicate.</param>
	/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, -1.</returns>
	public int FindIndex<T>(Predicate<T> match)
		where T : IWikiNode => this.FindIndex(item => item is T typedItem && match(typedItem));

	/// <summary>Finds the index of the first node of type T that satisfies the predicate.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="startIndex">The index at which to start the search.</param>
	/// <param name="match">The predicate to match.</param>
	/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, -1.</returns>
	public int FindIndex<T>(int startIndex, Predicate<T> match)
		where T : IWikiNode => this.FindIndex(startIndex, item => item is T typedItem && match(typedItem));

	/// <summary>Finds the index of the next node of the specified type.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="startIndex">The start index.</param>
	/// <returns>The zero-based index of the next node of the specified type, if found; otherwise, -1.</returns>
	/// <remarks>This method will not throw an exception if <paramref name="startIndex"/> is beyond the end of the collection. This is for convenience if the previous item of the same type was the last node in the collection.</remarks>
	public int FindIndex<T>(int startIndex)
		where T : IWikiNode => (startIndex >= this.Count) ? -1 : this.FindIndex(startIndex, item => item is T);

	/// <summary>Finds the last index of the first node of type T that satisfies the predicate.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="match">The predicate.</param>
	/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, -1.</returns>
	public int FindLastIndex<T>(Predicate<T> match)
		where T : IWikiNode => this.FindLastIndex(item => item is T typedItem && match(typedItem));

	/// <summary>Finds the last index of the first node of type T that satisfies the predicate.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="startIndex">The index at which to start the search.</param>
	/// <param name="match">The predicate to match.</param>
	/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match"/>, if found; otherwise, -1.</returns>
	public int FindLastIndex<T>(int startIndex, Predicate<T> match)
		where T : IWikiNode => this.FindLastIndex(startIndex, item => item is T typedItem && match(typedItem));

	/// <summary>Finds the last index of the next node of the specified type.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="startIndex">The start index.</param>
	/// <returns>The zero-based index of the next node of the specified type, if found; otherwise, -1.</returns>
	/// <remarks>This method will not throw an exception if <paramref name="startIndex"/> is beyond the end of the collection. This is for convenience if the previous item of the same type was the last node in the collection.</remarks>
	public int FindLastIndex<T>(int startIndex)
		where T : IWikiNode => (startIndex >= this.Count) ? -1 : this.FindLastIndex(startIndex, item => item is T);

	/// <summary>Finds a single link node, non-recursively, that satisfies the condition.</summary>
	/// <param name="condition">The condition the link node must satisfy.</param>
	/// <returns>The first link node that satisfies the condition.</returns>
	/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
	public ILinkNode? FindLink(Predicate<ILinkNode> condition) => this.Find(condition);

	/// <summary>Gets the <see cref="ILinkNode"/>s on the page.</summary>
	/// <param name="condition">The condition to match.</param>
	/// <value>The header nodes.</value>
	public IEnumerable<ILinkNode> FindLinks(Predicate<ILinkNode> condition) => this.FindAll<ILinkNode>(condition);

	/// <summary>Finds a single link node, non-recursively, that satisfies the condition.</summary>
	/// <param name="condition">The condition the link node must satisfy.</param>
	/// <returns>The first link node that satisfies the condition.</returns>
	/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
	public ITemplateNode? FindTemplate(Predicate<ITemplateNode> condition) => this.Find(condition);

	/// <summary>Gets the <see cref="ILinkNode"/>s on the page.</summary>
	/// <param name="condition">The condition to match.</param>
	/// <value>The header nodes.</value>
	public IEnumerable<ITemplateNode> FindTemplates(Predicate<ITemplateNode> condition) => this.FindAll<ITemplateNode>(condition);

	/// <summary>Replaces all current content with the content of the sections provided.</summary>
	/// <param name="sections">The new sections for the page.</param>
	public void FromSections(IEnumerable<Section> sections)
	{
		ArgumentNullException.ThrowIfNull(sections);
		this.Clear();
		foreach (var section in sections)
		{
			if (section.Header is IHeaderNode header)
			{
				this.Add(header);
			}

			this.AddRange(section.Content);
		}
	}

	/// <summary>Determines if the collection has any nodes of the specified type.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <returns><see langword="true"/> if any nodes of the specified type were found; otherwise, <see langword="false"/>.</returns>
	public bool Has<T>()
		where T : IWikiNode => this.Find<T>() is not null;

	/// <summary>Determines if the collection has any nodes of the specified type that satisfy the conditions given.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="condition">The condition a given node must satisfy.</param>
	/// <returns><see langword="true"/> if any nodes of the specified type were found that satisfied the specified condition; otherwise, <see langword="false"/>.</returns>
	public bool Has<T>(Predicate<T>? condition)
		where T : class, IWikiNode => this.Find(condition, false, false, 0) is not null;

	/// <summary>Finds the first header with the specified text.</summary>
	/// <param name="headerText">Name of the header.</param>
	/// <returns>The first header with the specified text.</returns>
	/// <remarks>This is a temporary function until HeaderNode can be rewritten to work more like other nodes (i.e., without capturing trailing whitespace).</remarks>
	public int IndexOfHeader(string headerText) => this.FindIndex<IHeaderNode>(header => header.GetTitle(true).OrdinalEquals(headerText));

	/// <summary>Splits a page into its individual sections. </summary>
	/// <returns>An enumeration of the sections of the page.</returns>
	/// <remarks>It is the caller's responsibility to set the collection's <see cref="SectionCollection.Comparer">Comparer property</see> if needed.</remarks>
	public SectionCollection ToSections() => this.ToSections(6);

	/// <summary>Splits a page into its individual sections. </summary>
	/// <param name="level">Only split on sections of this level or less (i.e., a value of 2 splits on levels 1 and 2).</param>
	/// <returns>An enumeration of the sections of the page.</returns>
	/// <remarks>It is the caller's responsibility to set the collection's <see cref="SectionCollection.Comparer">Comparer property</see> if needed.</remarks>
	public SectionCollection ToSections(int level)
	{
		var sections = new SectionCollection(this.Factory, level);
		var section = new Section(null, new WikiNodeCollection(this.Factory));
		foreach (var node in this)
		{
			if (node is IHeaderNode header && header.Level <= level)
			{
				if (section.Header != null || section.Content.Count > 0)
				{
					sections.Add(section);
				}

				section = new Section(header, new WikiNodeCollection(this.Factory));
			}
			else
			{
				section.Content.Add(node);
			}
		}

		sections.Add(section);
		return sections;
	}

	/// <summary>Parses the provided text to the best of its ability before adding it to the current <see cref="WikiNodeCollection"/>.</summary>
	/// <remarks>Note that this parses <em>only</em> the text provided, so passing incomplete text for a node will result in incorrect nodes being added. For example, using AddParsed("[[Hello") and AddParsed("|Goodbye]])" will result in different nodes than using AddParsed("[[Hello|Goodbye]]").</remarks>
	/// <param name="index">This index at which to insert the text.</param>
	/// <param name="text">The text.</param>
	public void InsertParsed(int index, [Localizable(false)] string text)
	{
		var newNodes = this.Factory.Parse(text);
		this.InsertRange(index, newNodes);
	}

	/// <summary>Adds text to the end of the collection.</summary>
	/// <param name="index">This index at which to insert the text.</param>
	/// <param name="text">The text.</param>
	/// <remarks>Adds text to the final node in the collection if it's an <see cref="ITextNode"/>; otherwise, creates a text node (via the factory) with the specified text and adds it to the collection.</remarks>
	public void InsertText(int index, [Localizable(false)] string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (index > 0 && this.Count >= index && this[index - 1] is ITextNode node)
			{
				node.Text += text;
			}
			else
			{
				this.Insert(index, this.Factory.TextNode(text));
			}
		}
	}

	/// <summary>Merges any adjacent TextNodes in the collection.</summary>
	/// <param name="recursive">if set to <see langword="true"/>, merges the entire tree.</param>
	/// <remarks>While the parser does this while parsing wiki text, user manipulation can lead to multiple adjacent TextNodes. Use this function if you require your tree to be well formed, or before intensive operations if you believe it could be heavily fragmented.</remarks>
	public void MergeText(bool recursive)
	{
		// Count - 1 because we can't merge the last node with anything.
		for (var i = 0; i < this.Count - 1; i++)
		{
			if (this[i] is ITextNode currentText && this[i + 1] is ITextNode nextText)
			{
				nextText.Text = currentText.Text + nextText.Text;
				this.RemoveAt(i);
				i--;
			}
			else if (recursive && this[i] is IParentNode parent)
			{
				foreach (var childCollection in parent.NodeCollections)
				{
					childCollection.MergeText(recursive);
				}
			}
		}
	}

	/// <summary>Removes all nodes of the given type.</summary>
	/// <typeparam name="T">The type of node to remove. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	public void RemoveAll<T>()
		where T : IWikiNode => this.RemoveAll(node => node is T);

	/// <summary>Removes all nodes of the given type.</summary>
	/// <typeparam name="T">The type of node to find. Must be derived from <see cref="IWikiNode"/>.</typeparam>
	/// <param name="condition">The condition a given node must satisfy.</param>
	public void RemoveAll<T>(Predicate<T> condition)
		where T : IWikiNode => this.RemoveAll(node => node is T typedNode && condition(typedNode));

	/// <summary>Replaces the specified node with zero or more new nodes.</summary>
	/// <param name="replaceMethod">A function to replace a single node with a collection of new nodes.</param>
	/// <param name="searchReplacements">A value indicating whether replacement nodes should be searched for new matches.</param>
	/// <remarks>The replacement function should determine whether or not the current node will be replaced. If not, or if the function itself modified the list, it should return null; otherwise, it should return a new WikiNodeCollection that will replace the current node.
	/// </remarks>
	public void Replace(NodeReplacer replaceMethod, bool searchReplacements)
	{
		ArgumentNullException.ThrowIfNull(replaceMethod);
		for (var i = 0; i < this.Count; i++)
		{
			var currentNode = this[i];
			if (currentNode is IParentNode parent)
			{
				foreach (var childCollection in parent.NodeCollections)
				{
					childCollection.Replace(replaceMethod, searchReplacements);
				}
			}

			if (replaceMethod(currentNode) is ICollection<IWikiNode> newNodes)
			{
				this.RemoveAt(i);
				this.InsertRange(i, newNodes);
				if (searchReplacements)
				{
					i--;
				}
				else
				{
					i += newNodes.Count - 1;
				}
			}
		}
	}

	/// <summary>Replaces text found in all ITextNode nodes.</summary>
	/// <param name="oldValue">The text to look for.</param>
	/// <param name="newValue">The text that should replace <paramref name="oldValue"/> with.</param>
	/// <param name="comparisonType">The string comparison method to use.</param>
	/// <remarks>The replacement function should determine whether or not the current node will be replaced. If not, or if the function itself modified the list, it should return null; otherwise, it should return a new WikiNodeCollection that will replace the current node.
	/// </remarks>
	public void ReplaceText(string oldValue, string newValue, StringComparison comparisonType) => this.Replace(match => ReplaceTextPrivate(match, oldValue, newValue, comparisonType), false);

	/// <summary>Trims whitespace from the beginning and end of the collection.</summary>
	/// <remarks>Note that this is a naive implementation that only looks at the first and last nodes of the collection. It is therefore recommended that you use <see cref="MergeText(bool)"/> first. This implementation also does not recurse, although this is unlikely to be an issue except in corner cases (e.g., a header node with trailing whitespace as the last entry in the collection).</remarks>
	public void Trim()
	{
		this.TrimStart();
		this.TrimEnd();
	}

	/// <summary>Trims whitespace from the end of the collection.</summary>
	public void TrimEnd()
	{
		if (this.Count == 0)
		{
			return;
		}

		switch (this[^1])
		{
			case ICommentNode comment:
				comment.Comment = comment.Comment.TrimEnd();
				break;
			case IHeaderNode header:
				header.Comment?.TrimStart();
				break;
			case ITextNode text:
				text.Text = text.Text.TrimEnd();
				if (text.Text.Length == 0)
				{
					this.RemoveAt(this.Count - 1);
					this.TrimEnd();
				}

				break;
			default:
				break;
		}
	}

	/// <summary>Trims whitespace from the beginning of the collection.</summary>
	public void TrimStart()
	{
		if (this.Count == 0)
		{
			return;
		}

		switch (this[0])
		{
			case ICommentNode comment:
				comment.Comment = comment.Comment.TrimStart();
				break;
			case IHeaderNode header:
				header.Comment?.TrimStart();
				break;
			case ITextNode text:
				text.Text = text.Text.TrimStart();
				if (text.Text.Length == 0)
				{
					this.RemoveAt(0);
					this.TrimStart();
				}

				break;
			default:
				break;
		}
	}
	#endregion

	#region Private Static Methods
	private static IList<IWikiNode>? ReplaceTextPrivate(IWikiNode match, string from, string to, StringComparison comparison)
	{
		switch (match)
		{
			case ICommentNode comment:
				comment.Comment = comment.Comment.Replace(from, to, comparison);
				return [comment];
			case ITextNode text:
				text.Text = text.Text.Replace(from, to, comparison);
				return [text];
			default:
				return null;
		}
	}
	#endregion
}
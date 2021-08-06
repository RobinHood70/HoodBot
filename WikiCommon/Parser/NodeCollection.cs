namespace RobinHood70.WikiCommon.Parser
{
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
	/// <returns>A LinkedListNode&lt;IWikiNode>.</returns>
	public delegate NodeCollection? NodeReplacer(IWikiNode node);

	/// <summary>A collection of <see cref="IWikiNode"/>s representing wiki text. Implemented as a linked list.</summary>
	/// <seealso cref="LinkedList{T}" />
	public class NodeCollection : List<IWikiNode>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="NodeCollection"/> class.</summary>
		/// <param name="factory">The factory to use to create new nodes.</param>
		public NodeCollection(IWikiNodeFactory? factory) => this.Factory = factory ?? new Basic.WikiNodeFactory();

		/// <summary>Initializes a new instance of the <see cref="NodeCollection"/> class.</summary>
		/// <param name="factory">The factory to use to create new nodes.</param>
		/// <param name="nodes">The nodes to initialize the collection with.</param>
		public NodeCollection(IWikiNodeFactory? factory, IEnumerable<IWikiNode> nodes)
			: base(nodes) => this.Factory = factory ?? new Basic.WikiNodeFactory();
		#endregion

		#region Public Properties

		/// <summary>Gets the <see cref="IWikiNodeFactory"/> used to create new nodes.</summary>
		/// <value>The factory.</value>
		public IWikiNodeFactory Factory { get; }
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

		/// <summary>Creates a shallow copy of the collection.</summary>
		/// <returns>A shallow copy of all nodes in the collection, along with the factory.</returns>
		public NodeCollection Clone()
		{
			IWikiNode[]? nodes = new IWikiNode[this.Count];
			this.CopyTo(nodes);
			return new NodeCollection(this.Factory, nodes);
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
		/// <remarks>The replacement function should determine whether or not the current node will be replaced. If not, or if the function itself modified the list, it should return null; otherwise, it should return a new NodeCollection that will replace the current node.
		/// </remarks>
		public void Replace(NodeReplacer replaceMethod, bool searchReplacements)
		{
			replaceMethod.ThrowNull(nameof(replaceMethod));
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

		/// <summary>Converts the <see cref="NodeCollection"/> to raw text.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public string ToRaw() => WikiTextVisitor.Raw(this);

		/// <summary>Converts the <see cref="NodeCollection"/> to it's value text.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public string ToValue() => WikiTextVisitor.Value(this);
		#endregion
	}
}
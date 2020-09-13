namespace RobinHood70.WikiCommon.Parser
{
	// TODO: Build a custom LinkedList replacement that's observable in some fashion.
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Properties;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>  A delegate for the method required by the Replace method.</summary>
	/// <param name="node">The node.</param>
	/// <returns>A LinkedListNode&lt;IWikiNode>.</returns>
	public delegate NodeCollection? NodeReplacer(LinkedListNode<IWikiNode> node);

	/// <summary>A collection of <see cref="IWikiNode"/>s representing wiki text. Implemented as a linked list.</summary>
	/// <seealso cref="LinkedList{T}" />
	public class NodeCollection : LinkedList<IWikiNode>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="NodeCollection"/> class.</summary>
		/// <param name="factory">The factory to use to create new nodes.</param>
		public NodeCollection(IWikiNodeFactory? factory) => this.Factory = factory ?? new Basic.WikiNodeFactory();
		#endregion

		#region Public Properties

		/// <summary>Gets the <see cref="IWikiNodeFactory"/> used to create new nodes.</summary>
		/// <value>The factory.</value>
		public IWikiNodeFactory Factory { get; }

		/// <summary>Gets the linked list nodes.</summary>
		/// <value>The linked list nodes.</value>
		public IEnumerable<LinkedListNode<IWikiNode>> LinkedNodes
		{
			get
			{
				for (var node = this.First; node != null; node = node.Next)
				{
					yield return node;
				}
			}
		}

		/// <summary>Gets the linked list nodesin reverse order.</summary>
		/// <value>The linked list nodes.</value>
		public IEnumerable<LinkedListNode<IWikiNode>> LinkedNodesReverse
		{
			get
			{
				for (var node = this.Last; node != null; node = node.Previous)
				{
					yield return node;
				}
			}
		}
		#endregion

		#region Public Static Methods

		/// <summary>Returns the value of all nodes between the start and end nodes, non-inclusive. The start and end nodes are assumed to be in the correct order.</summary>
		/// <param name="start">The start node.</param>
		/// <param name="end">The end node.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of the values between the two nodes.</returns>
		/// <exception cref="InvalidOperationException">The start and end nodes are not part of the same list.</exception>
		public static IEnumerable<IWikiNode> NodesBetween(LinkedListNode<IWikiNode> start, LinkedListNode<IWikiNode> end) => NodesBetween(start, end, false, false, false);

		/// <summary>Returns the value of all nodes between the start and end nodes, non-inclusive.</summary>
		/// <param name="start">The start node.</param>
		/// <param name="end">The end node.</param>
		/// <param name="checkOrder">if set to <c>true</c> checks the order of the nodes before proceeding.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of the values between the two nodes.</returns>
		/// <exception cref="InvalidOperationException">The start and end nodes are not part of the same list.</exception>
		public static IEnumerable<IWikiNode> NodesBetween(LinkedListNode<IWikiNode> start, LinkedListNode<IWikiNode> end, bool checkOrder) => NodesBetween(start, end, checkOrder, false, false);

		/// <summary>Returns the value of all nodes between the start and end nodes, non-inclusive.</summary>
		/// <param name="start">The start node.</param>
		/// <param name="end">The end node.</param>
		/// <param name="checkOrder">if set to <c>true</c> checks the order of the nodes before proceeding.</param>
		/// <param name="includeStart">if set to <c>true</c> [include start].</param>
		/// <param name="includeEnd">if set to <c>true</c> [include end].</param>
		/// <returns>An <see cref="IEnumerable{T}"/> of the values between the two nodes.</returns>
		/// <exception cref="InvalidOperationException">The start and end nodes are not part of the same list.</exception>
		public static IEnumerable<IWikiNode> NodesBetween(LinkedListNode<IWikiNode> start, LinkedListNode<IWikiNode> end, bool checkOrder, bool includeStart, bool includeEnd)
		{
			ThrowNull(start, nameof(start));
			ThrowNull(end, nameof(end));
			if (start.List != end.List)
			{
				throw new InvalidOperationException(Resources.NodesInDifferentLists);
			}

			if (checkOrder)
			{
				(start, end) = EnsureOrder(start, end);
			}

			if (includeStart)
			{
				yield return start.Value;
			}

			var current = start.Next;
			while (current != null && current != end)
			{
				yield return current.Value;
				current = current.Next;
			}

			if (includeEnd)
			{
				yield return end.Value;
			}
		}

		/// <summary>Normalizes the specified text.</summary>
		/// <param name="text">The text to normalize.</param>
		/// <remarks>Numerous parts of the parser rely on linebreaks being <c>\n</c>. This method provides offers a way to ensure that line endings conform to that expectation. This also removes null characters because while the parser can handle them fine, C# doesn't do so well with them in terms of displaying strings and such, and there really is no reason you should have null characters in wikitext anyway.</remarks>
		/// <returns>The normalized text.</returns>
		public static string NormalizeText(string text) => RegexLibrary.NewLinesToLineFeed(text).Replace("\0", string.Empty, StringComparison.Ordinal);

		/// <summary>Removes all nodes in the list after the given node.</summary>
		/// <param name="startAt">The node to start at.</param>
		public static void RemoveAfter(LinkedListNode<IWikiNode> startAt) => RemoveAfter(startAt, false);

		/// <summary>Removes all nodes in the list after the given node, optionally including the start node.</summary>
		/// <param name="startAt">The node to start at.</param>
		/// <param name="inclusive">if set to <see langword="true"/>, the start node will also be removed.</param>
		public static void RemoveAfter(LinkedListNode<IWikiNode> startAt, bool inclusive)
		{
			ThrowNull(startAt, nameof(startAt));
			ThrowNull(startAt.List, nameof(startAt), nameof(startAt.List));
			while (startAt.Next != null)
			{
				startAt.List.Remove(startAt.Next);
			}

			if (inclusive)
			{
				startAt.List.Remove(startAt);
			}
		}

		/// <summary>Removes all nodes in the list before the given node.</summary>
		/// <param name="start">The start.</param>
		public static void RemoveBefore(LinkedListNode<IWikiNode> start) => RemoveBefore(start, false);

		/// <summary>Removes all nodes in the list before the given node, optionally including the start node.</summary>
		/// <param name="start">The start.</param>
		/// <param name="inclusive">if set to <see langword="true"/>, the start node will also be removed.</param>
		public static void RemoveBefore(LinkedListNode<IWikiNode> start, bool inclusive)
		{
			ThrowNull(start, nameof(start));
			ThrowNull(start.List, nameof(start), nameof(start.List));
			while (start.Previous != null)
			{
				start.List.Remove(start.Previous);
			}

			if (inclusive)
			{
				start.List.Remove(start);
			}
		}

		/// <summary>Removes all nodes between the start and end nodes, not including either node.</summary>
		/// <param name="start">The start node.</param>
		/// <param name="end">The end node.</param>
		/// <param name="checkOrder">if set to <see langword="true"/>, the method will verify whether the end node comes before or after the start node, and adjust accordingly; if set to <see langword="false"/>, the check will be skipped. It is safe to set this to <see langword="false"/> if you can be certain that the end node comes after the start node (e.g., it was found by using the <see cref="LinkedListNode{T}.Next">Next</see> property or one of the <c>FindNext</c> methods); otherwise, leave it set to <see langword="true"/> or else all entries from the start node forward will be removed.</param>
		/// <exception cref="InvalidOperationException">The start and end nodes are not part of the same list.</exception>
		public static void RemoveBetween(LinkedListNode<IWikiNode> start, LinkedListNode<IWikiNode> end, bool checkOrder)
		{
			ThrowNull(start, nameof(start));
			ThrowNull(end, nameof(end));
			ThrowNull(start.List, nameof(start), nameof(start.List));
			if (start.List != end.List)
			{
				throw new InvalidOperationException(Resources.NodesInDifferentLists);
			}

			if (start == end)
			{
				return;
			}

			if (checkOrder)
			{
				(start, end) = EnsureOrder(start, end);
			}

			if (start.List is LinkedList<IWikiNode> list)
			{
				while (start.Next != null && start.Next != end)
				{
					list.Remove(start.Next);
				}
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Adds all nodes in the provided collection to the end of the current collection.</summary>
		/// <param name="collection">The collection to be added.</param>
		public void AddRange(IEnumerable<IWikiNode> collection)
		{
			ThrowNull(collection, nameof(collection));
			foreach (var item in collection)
			{
				this.AddLast(item);
			}
		}

		/// <summary>Adds text to the end of the collection.</summary>
		/// <param name="text">The text.</param>
		/// <remarks>Adds text to the existing node, if the last node in the collection is a TextNode; otherwise, creates a CreateTextNode with the specified text and adds it to the collection.</remarks>
		public void AddText([Localizable(false)] string text)
		{
			if (this.Last is LinkedListNode<IWikiNode> last && last.Value is ITextNode node)
			{
				node.Text += text;
			}
			else
			{
				node = this.Factory.TextNode(text);
				this.AddLast(node);
			}
		}

		/// <summary>Finds a single node of the specified type that satisfies the condition.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
		/// <param name="reverse">Set to <see langword="true"/> to reverse the search direction.</param>
		/// <param name="recursive">Set to <see langword="true"/> to search all nodes recursively; otherwise, only the top level of nodes will be searched.</param>
		/// <param name="startAt">The node to start searching at (inclusive). May be <see langword="null"/>.</param>
		/// <returns>The first node (or the last, for reverse searches) in the collection that is of the specified type and satisfies the condition.</returns>
		/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
		public T? Find<T>(Predicate<T>? condition, bool reverse, bool recursive, LinkedListNode<IWikiNode>? startAt)
			where T : class, IWikiNode
		{
			var firstNode = startAt ?? (reverse ? this.Last : this.First);
			for (var node = firstNode; node != null; node = reverse ? node.Previous : node.Next)
			{
				if (node.Value is T typedNode && (condition == null || condition(typedNode)))
				{
					return typedNode;
				}

				if (recursive && node.Value is IParentNode parent)
				{
					foreach (var childCollection in parent.NodeCollections)
					{
						if (childCollection.Find(condition, recursive, reverse, null) is T found)
						{
							return found;
						}
					}
				}
			}

			return default;
		}

		/// <summary>Finds all nodes of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		/// <remarks>Outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned before the <c>{{Example|example}}</c> template.</remarks>
		public IEnumerable<T> FindAll<T>()
			where T : class, IWikiNode => this.FindAll<T>(null, false, true, null);

		/// <summary>Finds all nodes of the specified type that satisfy the condition.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <param name="reverse">Set to <see langword="true"/> to reverse the search direction.</param>
		/// <param name="recursive">Set to <see langword="true"/> to search all nodes recursively; otherwise, only the top level of nodes will be searched.</param>
		/// <param name="startAt">The node to start searching at (inclusive). May be <see langword="null"/>.</param>
		/// <returns>The nodes in the collection that are of the specified type and satisfy the condition.</returns>
		/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned before the <c>{{Example|example}}</c> template.</remarks>
		public IEnumerable<T> FindAll<T>(Predicate<T>? condition, bool reverse, bool recursive, LinkedListNode<IWikiNode>? startAt)
			where T : class, IWikiNode
		{
			var firstNode = startAt ?? (reverse ? this.Last : this.First);
			for (var node = firstNode; node != null; node = reverse ? node.Previous : node.Next)
			{
				if (node.Value is T typedNode && (condition == null || condition(typedNode)))
				{
					yield return typedNode;
				}

				if (recursive && node.Value is IParentNode parent)
				{
					foreach (var childCollection in parent.NodeCollections)
					{
						foreach (var value in childCollection.FindAll(condition, recursive, reverse, null))
						{
							yield return value;
						}
					}
				}
			}
		}

		/// <summary>Finds all <see cref="LinkedListNode{T}"/>s with a <see cref="LinkedListNode{T}.Value">Value</see> of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		/// <remarks>Outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned before the <c>{{Example|example}}</c> template.</remarks>
		public IEnumerable<LinkedListNode<IWikiNode>> FindAllListNodes<T>()
			where T : class, IWikiNode => this.FindAllListNodes<T>(null, false, true, null);

		/// <summary>Finds all <see cref="LinkedListNode{T}"/>s with a <see cref="LinkedListNode{T}.Value">Value</see> of the specified type that satisfy the condition.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
		/// <param name="reverse">Set to <see langword="true"/> to reverse the search direction.</param>
		/// <param name="recursive">Set to <see langword="true"/> to search all nodes recursively; otherwise, only the top level of nodes will be searched.</param>
		/// <param name="startAt">The node to start searching at (inclusive). May be <see langword="null"/>.</param>
		/// <returns>The first node (or the last, for reverse searches) in the collection that is of the specified type and satisfies the condition.</returns>
		/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
		public IEnumerable<LinkedListNode<IWikiNode>> FindAllListNodes<T>(Predicate<T>? condition, bool reverse, bool recursive, LinkedListNode<IWikiNode>? startAt)
			where T : class, IWikiNode
		{
			var firstNode = startAt ?? (reverse ? this.Last : this.First);
			for (var node = firstNode; node != null; node = reverse ? node.Previous : node.Next)
			{
				if (node.Value is T typedNode && (condition == null || condition(typedNode)))
				{
					yield return node;
				}

				if (recursive && node.Value is IParentNode parent)
				{
					foreach (var childCollection in parent.NodeCollections)
					{
						foreach (var value in childCollection.FindAllListNodes(condition, recursive, reverse, null))
						{
							yield return value;
						}
					}
				}
			}
		}

		/// <summary>Finds a single <see cref="LinkedListNode{T}"/> of the specified type that satisfies the condition.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy. If set to <see langword="null"/>, the first node that satisfies the reamining parameters will be returned.</param>
		/// <param name="reverse">Set to <see langword="true"/> to reverse the search direction.</param>
		/// <param name="recursive">Set to <see langword="true"/> to search all nodes recursively; otherwise, only the top level of nodes will be searched.</param>
		/// <param name="startAt">The node to start searching at (inclusive). May be <see langword="null"/>.</param>
		/// <returns>The first node (or the last, for reverse searches) in the collection that is of the specified type and satisfies the condition.</returns>
		/// <remarks>For recursive searches, outer nodes that satisfy the condition are returned before inner nodes that satisfy the condition. For example, if searching for the template <c>{{Example}}</c> in the wiki code <c>{{Example|This is an embedded {{Example|example}}.}}</c>, the <c>{{Example|This is...}}</c> template will be returned, not the <c>{{Example|example}}</c> template.</remarks>
		public LinkedListNode<IWikiNode>? FindListNode<T>(Predicate<T>? condition, bool reverse, bool recursive, LinkedListNode<IWikiNode>? startAt)
			where T : class, IWikiNode
		{
			var firstNode = startAt ?? (reverse ? this.Last : this.First);
			for (var node = firstNode; node != null; node = reverse ? node.Previous : node.Next)
			{
				if (node.Value is T typedNode && (condition == null || condition(typedNode)))
				{
					return node;
				}

				if (recursive && node.Value is IParentNode parent)
				{
					foreach (var childCollection in parent.NodeCollections)
					{
						if (childCollection.FindListNode(condition, recursive, reverse, null) is LinkedListNode<IWikiNode> found)
						{
							return found;
						}
					}
				}
			}

			return default;
		}

		/// <summary>Merges any adjacent TextNodes in the collection.</summary>
		/// <param name="recursive">if set to <see langword="true"/>, merges the entire tree.</param>
		/// <remarks>While the parser does this while parsing wiki text, user manipulation can lead to multiple adjacent TextNodes. Use this function if you require your tree to be well formed, or before intensive operations if you believe it could be heavily fragmented.</remarks>
		public void MergeText(bool recursive)
		{
			// TODO: Re-write as foreach.LinkedNode or similar.
			var current = this.First;
			while (current != null)
			{
				var next = current.Next;
				if (current.Value is ITextNode currentText && next?.Value is ITextNode nextText)
				{
					nextText.Text = currentText.Text + nextText.Text;
					this.Remove(current);
				}
				else if (recursive && current.Value is IParentNode parent)
				{
					foreach (var childCollection in parent.NodeCollections)
					{
						childCollection.MergeText(recursive);
					}
				}

				current = next;
			}
		}

		/// <summary>Removes all nodes of the given type.</summary>
		/// <typeparam name="T">The type of node to remove.</typeparam>
		public void RemoveAll<T>()
			where T : IWikiNode => this.RemoveAll<T>(node => true);

		/// <summary>Removes all nodes of the given type.</summary>
		/// <typeparam name="T">The type of node to remove.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		public void RemoveAll<T>(Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			var currentNode = this.First;
			while (currentNode != null)
			{
				if (currentNode.Value is T castNode && condition(castNode))
				{
					if (currentNode.List == null)
					{
						throw new InvalidOperationException();
					}

					currentNode.List.Remove(currentNode);
				}

				currentNode = currentNode.Next;
			}
		}

		/// <summary>Replaces the specified node with zero or more new nodes.</summary>
		/// <param name="replaceMethod">A function to replace a single node with a collection of new nodes.</param>
		/// <param name="searchReplacements">A value indicating whether replacement nodes should be searched for new matches.</param>
		/// <remarks>The replacement function should determine whether or not the current node will be replaced. If not, or if the function itself modified the list, it should return null; otherwise, it should return a new NodeCollection that will replace the current node.
		/// </remarks>
		public void Replace(NodeReplacer replaceMethod, bool searchReplacements)
		{
			ThrowNull(replaceMethod, nameof(replaceMethod));
			var currentNode = this.First;
			while (currentNode != null)
			{
				if (currentNode.Value is IParentNode parent)
				{
					foreach (var childCollection in parent.NodeCollections)
					{
						childCollection.Replace(replaceMethod, searchReplacements);
					}
				}

				if (replaceMethod(currentNode) is NodeCollection newNodes)
				{
					this.Remove(currentNode);
					foreach (var colNode in newNodes)
					{
						if (searchReplacements)
						{
							this.AddAfter(currentNode, colNode);
						}
						else
						{
							this.AddBefore(currentNode, colNode);
						}
					}
				}

				currentNode = currentNode.Next;
			}
		}
		#endregion

		#region Private Static Methods
		private static (LinkedListNode<IWikiNode> Start, LinkedListNode<IWikiNode> End) EnsureOrder(LinkedListNode<IWikiNode> start, LinkedListNode<IWikiNode> end)
		{
			var current = start;
			while (current.Next != null && current.Next != end)
			{
				current = current.Next;
			}

			return current.Next == null ? (end, start) : (start, end);
		}
		#endregion
	}
}
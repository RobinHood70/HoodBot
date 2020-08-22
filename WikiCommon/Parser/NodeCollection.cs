namespace RobinHood70.WikiCommon.Parser
{
	// TODO: Move most of the static functions to extension methods (and throw errors if not in the same list, instead of allowing any list).
	// TODO: Add recursive options to all Find methods. (See Replace method for the very simple algorithm to do so.)
	// TODO: Build a custom LinkedList replacement that's observable in some fashion.
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using RobinHood70.WikiCommon.Properties;
	using static RobinHood70.CommonCode.Globals;

	// CONSIDER: Implementing a NodeCollection<T> so that properties like Parameters can be more strongly typed as NodeCollection<ParameterNode>.

	/// <summary>  A delegate for the method required by the Replace method.</summary>
	/// <param name="node">The node.</param>
	/// <returns>A LinkedListNode&lt;IWikiNode>.</returns>
	public delegate NodeCollection? NodeReplacer(LinkedListNode<IWikiNode> node);

	/// <summary>Represents a collection of <see cref="IWikiNode"/> nodes.</summary>
	public class NodeCollection : LinkedList<IWikiNode>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="NodeCollection"/> class.</summary>
		/// <param name="parent">The parent.</param>
		public NodeCollection(IWikiNode? parent)
			: base() => this.Parent = parent;

		/// <summary>Initializes a new instance of the <see cref="NodeCollection"/> class.</summary>
		/// <param name="parent">The parent.</param>
		/// <param name="nodes">The nodes.</param>
		public NodeCollection(IWikiNode? parent, IEnumerable<IWikiNode> nodes)
			: this(parent)
		{
			ThrowNull(nodes, nameof(nodes));
			foreach (var node in nodes)
			{
				this.AddLast(node);
			}
		}
		#endregion

		#region Public Properties

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

		/// <summary>Gets the parent node for the collection.</summary>
		/// <value>The node's parent, or <see langword="null"/> if this is the root node.</value>
		public IWikiNode? Parent { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Finds the next <see cref="LinkedListNode{T}">LinkedListNode</see> of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="startAt">The node to start searching from. This node <i>will</i> be included in the search.</param>
		/// <returns>The next node in the collection of the specified type.</returns>
		public static LinkedListNode<IWikiNode>? FindNextLinked<T>(LinkedListNode<IWikiNode>? startAt)
			where T : IWikiNode => FindNextLinked(startAt, (T item) => true);

		/// <summary>Finds the next <see cref="LinkedListNode{T}">LinkedListNode</see> of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="startAt">The node to start searching from. This node <i>will</i> be included in the search.</param>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The next node in the collection of the specified type.</returns>
		public static LinkedListNode<IWikiNode>? FindNextLinked<T>(LinkedListNode<IWikiNode>? startAt, Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			var node = startAt;
			while (node != null)
			{
				if (node.Value is T castNode && condition(castNode))
				{
					return node;
				}

				node = node.Next;
			}

			return null;
		}

		/// <summary>Finds the previous <see cref="LinkedListNode{T}">LinkedListNode</see> of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="startAt">The node to start searching from. This node <i>will</i> be included in the search.</param>
		/// <returns>The previous node in the collection of the specified type.</returns>
		public static LinkedListNode<IWikiNode>? FindPreviousLinked<T>(LinkedListNode<IWikiNode>? startAt)
			where T : IWikiNode => FindPreviousLinked(startAt, (T item) => true);

		/// <summary>Finds the previous <see cref="LinkedListNode{T}">LinkedListNode</see> of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="startAt">The node to start searching from. This node <i>will</i> be included in the search.</param>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The previous node in the collection of the specified type.</returns>
		public static LinkedListNode<IWikiNode>? FindPreviousLinked<T>(LinkedListNode<IWikiNode>? startAt, Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			var node = startAt;
			while (node != null)
			{
				if (node.Value is T castNode && condition(castNode))
				{
					return node;
				}

				node = node.Previous;
			}

			return null;
		}

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

		/// <summary>Removes all nodes in the list after the given node.</summary>
		/// <param name="start">The start.</param>
		public static void RemoveAfter(LinkedListNode<IWikiNode> start) => RemoveAfter(start, false);

		/// <summary>Removes all nodes in the list after the given node, optionally including the start node.</summary>
		/// <param name="start">The start.</param>
		/// <param name="inclusive">if set to <see langword="true"/>, the start node will also be removed.</param>
		public static void RemoveAfter(LinkedListNode<IWikiNode> start, bool inclusive)
		{
			ThrowNull(start, nameof(start));
			ThrowNull(start.List, nameof(start), nameof(start.List));
			while (start.Next != null)
			{
				start.List.Remove(start.Next);
			}

			if (inclusive)
			{
				start.List.Remove(start);
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
		/// <remarks>Adds text to the existing node, if the last node in the collection is a TextNode; otherwise, creates a new TextNode with the specified text and adds it to the collection.</remarks>
		public void AddText([Localizable(false)] string text)
		{
			if (this.Last is LinkedListNode<IWikiNode> last && last.Value is TextNode node)
			{
				node.Text += text;
			}
			else
			{
				node = new TextNode(text);
				this.AddLast(node);
			}
		}

		/// <summary>Finds all nodes in the collection satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The nodes that satisfy the specified condition.</returns>
		public IEnumerable<IWikiNode> FindAll(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this)
			{
				if (condition(node))
				{
					yield return node;
				}
			}
		}

		/// <summary>Finds all nodes of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		public IEnumerable<T> FindAll<T>()
			where T : IWikiNode => this.FindAll((T item) => true);

		/// <summary>Finds all nodes of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		public IEnumerable<T> FindAll<T>(Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this)
			{
				if (node is T castNode && condition(castNode))
				{
					yield return castNode;
				}
			}
		}

		/// <summary>Finds all nodes in the collection satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The nodes that satisfy the specified condition.</returns>
		public IEnumerable<IWikiNode> FindAllRecursive(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this)
			{
				if (condition(node))
				{
					yield return node;
				}

				if (node.NodeCollections != null)
				{
					foreach (var subNode in node.NodeCollections)
					{
						foreach (var value in subNode.FindAllRecursive(condition))
						{
							yield return value;
						}
					}
				}
			}
		}

		/// <summary>Finds all nodes of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		public IEnumerable<T> FindAllRecursive<T>()
			where T : IWikiNode => this.FindAllRecursive((T item) => true);

		/// <summary>Finds all nodes of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		public IEnumerable<T> FindAllRecursive<T>(Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			for (var node = this.First; node != null; node = node.Next)
			{
				if (node.Value is T castNode && condition(castNode))
				{
					yield return castNode;
				}

				if (node.Value.NodeCollections != null)
				{
					foreach (var subNode in node.Value.NodeCollections)
					{
						foreach (var value in subNode.FindAllRecursive(condition))
						{
							yield return value;
						}
					}
				}
			}
		}

		/// <summary>Finds all <see cref="LinkedListNode{T}">LinkedListNodes</see> satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The nodes in the collection that satisfy the specified condition.</returns>
		/// <remarks>This version allows you to traverse and/or modify the list as needed. Unlike the Replace method, however, this method is unaware of any alterations you make to the collection, so will traverse the nodes in the order they're in, regardless of any changes. Anything that modifies the Next property of the current node will inherently also alter the iteration. If you insert nodes after the current node, they will be iterated through.</remarks>
		public IEnumerable<LinkedListNode<IWikiNode>> FindAllLinked(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this.LinkedNodes)
			{
				if (condition(node.Value))
				{
					yield return node;
				}
			}
		}

		/// <summary>Finds all <see cref="LinkedListNode{T}">LinkedListNodes</see> with values of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		/// <remarks>This version allows you to traverse and/or modify the list as needed. Unlike the Replace method, however, this method is unaware of any alterations you make to the collection, so will traverse the nodes in the order they're in, regardless of any changes. Anything that modifies the Next property of the current node will inherently also alter the iteration. If you insert nodes after the current node, they will be iterated through.</remarks>
		public IEnumerable<LinkedListNode<IWikiNode>> FindAllLinked<T>()
			where T : IWikiNode => this.FindAllLinked((T item) => true);

		/// <summary>Finds all <see cref="LinkedListNode{T}">LinkedListNodes</see> with values of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The nodes in the collection that are of the specified type.</returns>
		/// <remarks>This version allows you to traverse and/or modify the list as needed. Unlike the Replace method, however, this method is unaware of any alterations you make to the collection, so will traverse the nodes in the order they're in, regardless of any changes. Anything that modifies the Next property of the current node will inherently also alter the iteration. If you insert nodes after the current node, they will be iterated through.</remarks>
		public IEnumerable<LinkedListNode<IWikiNode>> FindAllLinked<T>(Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this.LinkedNodes)
			{
				if (node.Value is T castNode && condition(castNode))
				{
					yield return node;
				}
			}
		}

		/// <summary>Finds the first node in the collection satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The first node that satisfies the specified condition.</returns>
		public IWikiNode? FindFirst(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this.LinkedNodes)
			{
				if (condition(node.Value))
				{
					return node.Value;
				}
			}

			return default;
		}

		/// <summary>Finds the first node of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The first node node in the collection of the specified type.</returns>
		public T? FindFirst<T>()
			where T : class, IWikiNode => this.FindFirst((T item) => true);

		/// <summary>Finds the first node of the specified type that satisfies the specified condition.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The first node node in the collection of the specified type.</returns>
		public T? FindFirst<T>(Predicate<T> condition)
			where T : class, IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this)
			{
				if (node is T castNode && condition(castNode))
				{
					return castNode;
				}
			}

			return default;
		}

		/// <summary>Finds the first <see cref="LinkedListNode{T}">LinkedListNode</see> satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The first node in the collection that satisfies the specified condition.</returns>
		public LinkedListNode<IWikiNode>? FindFirstLinked(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this.LinkedNodes)
			{
				if (condition(node.Value))
				{
					return node;
				}
			}

			return default;
		}

		/// <summary>Finds the first <see cref="LinkedListNode{T}">LinkedListNode</see> of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The first node in the collection of the specified type.</returns>
		public LinkedListNode<IWikiNode>? FindFirstLinked<T>()
			where T : IWikiNode => this.FindFirstLinked((T item) => true);

		/// <summary>Finds the first <see cref="LinkedListNode{T}">LinkedListNode</see> of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The first node in the collection of the specified type.</returns>
		public LinkedListNode<IWikiNode>? FindFirstLinked<T>(Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			foreach (var node in this.LinkedNodes)
			{
				if (node.Value is T castNode && condition(castNode))
				{
					return node;
				}
			}

			return default;
		}

		/// <summary>Finds the first header with the specified text.</summary>
		/// <param name="headerText">Name of the header.</param>
		/// <returns>The first header with the specified text.</returns>
		/// <remarks>This is a temporary function until HeaderNode can be rewritten to work more like other nodes (i.e., without capturing trailing whitespace).</remarks>
		public LinkedListNode<IWikiNode>? FindFirstHeaderLinked(string headerText) => this.FindFirstLinked<HeaderNode>(header => header.GetInnerText(true) == headerText);

		/// <summary>Finds the last node in the collection satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The last node that satisfies the specified condition.</returns>
		public IWikiNode? FindLast(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			var node = this.Last;
			while (node != null)
			{
				if (condition(node.Value))
				{
					return node.Value;
				}

				node = node.Previous;
			}

			return default;
		}

		/// <summary>Finds the last node of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The last node node in the collection of the specified type.</returns>
		public T? FindLast<T>()
			where T : class, IWikiNode => this.FindLast((T item) => true);

		/// <summary>Finds the last node of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The last node node in the collection of the specified type.</returns>
		public T? FindLast<T>(Predicate<T> condition)
			where T : class, IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			var node = this.Last;
			while (node != null)
			{
				if (node.Value is T lastNode && condition(lastNode))
				{
					return lastNode;
				}

				node = node.Previous;
			}

			return default;
		}

		/// <summary>Finds the last header with the specified text.</summary>
		/// <param name="headerText">Name of the header.</param>
		/// <returns>The first header with the specified text.</returns>
		/// <remarks>This is a temporary function until HeaderNode can be rewritten to work more like other nodes (i.e., without capturing trailing whitespace).</remarks>
		public LinkedListNode<IWikiNode>? FindLastHeaderLinked(string headerText) => this.FindLastLinked<HeaderNode>(header => header.GetInnerText(true) == headerText);

		/// <summary>Finds the last <see cref="LinkedListNode{T}">LinkedListNode</see> satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The last node in the collection that satisfy the specified condition.</returns>
		public LinkedListNode<IWikiNode>? FindLastLinked(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			var node = this.Last;
			while (node != null)
			{
				if (condition(node.Value))
				{
					return node;
				}

				node = node.Previous;
			}

			return null;
		}

		/// <summary>Finds the last <see cref="LinkedListNode{T}">LinkedListNode</see> with a value of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <returns>The last node in the collection of the specified type.</returns>
		public LinkedListNode<IWikiNode>? FindLastLinked<T>()
			where T : IWikiNode => this.FindLastLinked((T item) => true);

		/// <summary>Finds the last <see cref="LinkedListNode{T}">LinkedListNode</see> with a value of the specified type.</summary>
		/// <typeparam name="T">The type of node to find.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The last node in the collection of the specified type.</returns>
		public LinkedListNode<IWikiNode>? FindLastLinked<T>(Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			var node = this.Last;
			while (node != null)
			{
				if (node.Value is T castNode && condition(castNode))
				{
					return node;
				}

				node = node.Previous;
			}

			return null;
		}

		/// <summary>Gets the backing LinkedList's version field to provide add/remove tracking.</summary>
		/// <returns>The version.</returns>
		public int GetVersion()
		{
			var info = new SerializationInfo(typeof(NodeCollection), new FormatterConverter());
			this.GetObjectData(info, default);
			return info.GetInt32("Version");
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
				if (current.Value is TextNode currentText && next?.Value is TextNode nextText)
				{
					nextText.Text = currentText.Text + nextText.Text;
					this.Remove(current);
				}
				else if (recursive)
				{
					foreach (var node in current.Value.NodeCollections)
					{
						node.MergeText(recursive);
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
				foreach (var subnode in currentNode.Value.NodeCollections)
				{
					subnode.Replace(replaceMethod, searchReplacements);
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

		/// <summary>Replaces all nodes of the specified type with a single node inserted at the position of the first node of the specified type, or at the beginning of the collection if no nodes of the desired type are found.</summary>
		/// <typeparam name="T">The type of node to be removed. Constrained to <see cref="IWikiNode"/>.</typeparam>
		/// <param name="newNode">The new node.</param>
		/// <remarks>This will commonly be used to replace text that may be interspersed with non-text by a single value. See the example.</remarks>
		/// <example>Using <c>MyTemplateNode.Title.ReplaceAll&lt;TextNode&gt;(new TextNode("NewName"));</c> on a template like <c>{{&lt;!--Leading comment-->Template&lt;!--Interior comment-->Name}}</c> would produce <c>{{&lt;!--Leading comment-->NewName}}</c>.</example>
		public void ReplaceAllWithOne<T>(IWikiNode newNode)
			where T : IWikiNode
		{
			ThrowNull(newNode, nameof(newNode));
			var currentNode = this.First;
			var first = true;
			while (currentNode != null)
			{
				var nextNode = currentNode.Next;
				if (currentNode.Value is T)
				{
					if (first)
					{
						first = false;
						this.AddBefore(currentNode, newNode);
					}

					this.Remove(currentNode);
				}

				currentNode = nextNode;
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
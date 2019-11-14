namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using static WikiCommon.Globals;

	// CONSIDER: Implementing a NodeCollection<T> so that properties like Parameters can be more strongly typed as NodeCollection<ParameterNode>.

	/// <summary>  A delegate for the method required by the Replace method.</summary>
	/// <param name="node">The node.</param>
	/// <returns>A LinkedListNode&lt;IWikiNode>.</returns>
	public delegate IWikiNode? NodeReplacer(LinkedListNode<IWikiNode> node);

	/// <summary>Represents a collection of <see cref="IWikiNode"/> nodes.</summary>
	public class NodeCollection : LinkedList<IWikiNode>, IWikiNode
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
			: base(nodes) => this.Parent = parent;
		#endregion

		#region Public Properties

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <value>The node collections.</value>
		public IEnumerable<NodeCollection> NodeCollections => Enumerable.Empty<NodeCollection>();

		/// <summary>Gets the parent node for the collection.</summary>
		/// <value>The node's parent, or <see langword="null"/> if this is the root node.</value>
		public IWikiNode? Parent { get; }
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
			if (this.Last.Value is TextNode node)
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

		/// <summary>Finds all <see cref="LinkedListNode{T}">LinkedListNodes</see> satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The nodes in the collection that satisfy the specified condition.</returns>
		/// <remarks>This version allows you to traverse and/or modify the list as needed. Unlike the Replace method, however, this method is unaware of any alterations you make to the collection, so will traverse the nodes in the order they're in, regardless of any changes. Anything that modifies the Next property of the current node will inherently also alter the iteration. If you insert nodes after the current node, they will be iterated through.</remarks>
		public IEnumerable<LinkedListNode<IWikiNode>> FindAllLinked(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			var node = this.First;
			while (node != null)
			{
				if (condition(node.Value))
				{
					yield return node;
				}

				node = node.Next;
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
			var node = this.First;
			while (node != null)
			{
				if (node.Value is T castNode && condition(castNode))
				{
					yield return node;
				}

				node = node.Next;
			}
		}

		/// <summary>Finds the first node in the collection satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The first node that satisfies the specified condition.</returns>
		public IWikiNode? FindFirst(Predicate<IWikiNode> condition)
		{
			ThrowNull(condition, nameof(condition));
			var node = this.First;
			while (node != null)
			{
				if (condition(node.Value))
				{
					return node.Value;
				}

				node = node.Next;
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
			var node = this.First;
			while (node != null)
			{
				if (condition(node.Value))
				{
					return node;
				}

				node = node.Next;
			}

			return null;
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
			var node = this.First;
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

		/// <summary>Finds the last node in the collection satisfying the specified condition.</summary>
		/// <param name="condition">The condition a given node must satisfy.</param>
		/// <returns>The last node that satisfies the specified condition.</returns>
		public IWikiNode? FindLast(Predicate<IWikiNode> condition)
		{
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

		/// <summary>Merges any adjacent TextNodes in the collection.</summary>
		/// <param name="recursive">if set to <see langword="true"/>, merges the entire tree.</param>
		/// <remarks>While the parser does this while parsing wiki text, user manipulation can lead to multiple adjacent TextNodes. Use this function if you require your tree to be well formed, or before intensive operations if you believe it could be heavily fragmented.</remarks>
		public void MergeText(bool recursive)
		{
			var current = this.First;
			while (current != null)
			{
				var next = current.Next;
				if (current.Value is TextNode currentText && next.Value is TextNode nextText)
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
			where T : IWikiNode => this.Replace((node) => node.Value is T ? null : node.Value);

		/// <summary>Removes all nodes of the given type.</summary>
		/// <typeparam name="T">The type of node to remove.</typeparam>
		/// <param name="condition">The condition a given node must satisfy.</param>
		public void RemoveAll<T>(Predicate<T> condition)
			where T : IWikiNode
		{
			ThrowNull(condition, nameof(condition));
			this.Replace((node) => node.Value is T castNode && condition(castNode) ? null : node.Value);
		}

		/// <summary>Replaces the specified node with a new node.</summary>
		/// <param name="replaceMethod">A function to replace the value with a new one.</param>
		/// <remarks>The replacement function should determine whether or not the current node is the desired one, and then return one of the following:
		/// <list type="bullet">
		///     <item>The current <see cref="LinkedListNode{T}.Value">Value</see> of the <see cref="LinkedListNode{T}"/> if it is not the desired node or the node has already been modified by the replacer method.</item>
		///     <item>A single <see cref="IWikiNode"/> to replace the value of the original node with. If this is a <see cref="NodeCollection"/>, the original node will be removed, and the items of the collection inserted in its place.</item>
		///     <item><see langword="null"/> if the node should be removed.</item>
		/// </list>
		/// Any new nodes that are added will not be searched by the replacer.
		/// </remarks>
		public void Replace(NodeReplacer replaceMethod)
		{
			ThrowNull(replaceMethod, nameof(replaceMethod));
			var currentNode = this.First;
			while (currentNode != null)
			{
				foreach (var subnode in currentNode.Value.NodeCollections)
				{
					subnode.Replace(replaceMethod);
				}

				var newNode = replaceMethod(currentNode);
				var nextNode = currentNode.Next;
				if (!ReferenceEquals(currentNode.Value, newNode))
				{
					if (newNode == null)
					{
						this.Remove(currentNode);
					}
					else if (newNode is NodeCollection newNodes)
					{
						foreach (var colNode in newNodes)
						{
							this.AddBefore(currentNode, colNode);
						}

						this.Remove(currentNode);
					}
					else
					{
						currentNode.Value = newNode;
					}
				}

				currentNode = nextNode;
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
	}
}

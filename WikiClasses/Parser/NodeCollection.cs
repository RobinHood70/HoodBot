﻿namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections.Generic;
	using static WikiCommon.Globals;

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

		/// <summary>Removes all nodes of the given type.</summary>
		/// <typeparam name="T">The type of node to remove.</typeparam>
		public void RemoveAll<T>()
			where T : IWikiNode => this.Replace((node) => node.Value is T ? null : node.Value);

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
				if (currentNode.Value is IEnumerable<NodeCollection> tree)
				{
					foreach (var subnode in tree)
					{
						subnode.Replace(replaceMethod);
					}
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

					currentNode = nextNode;
				}
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

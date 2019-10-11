namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	public class NodeCollection : LinkedList<IWikiNode>, IWikiNode
	{
		#region Constructors
		public NodeCollection(IWikiNode parent)
			: base() => this.Parent = parent;

		public NodeCollection(IWikiNode parent, IEnumerable<IWikiNode> nodes)
			: base(nodes) => this.Parent = parent;
		#endregion

		#region Public Properties
		public IWikiNode Parent { get; }
		#endregion

		#region Public Methods
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

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
			where T : IWikiNode => this.Replace((node) => node is T ? null : node);

		/// <summary>Replaces the specified node with a new node.</summary>
		/// <param name="replaceMethod">A function to replace the value with a new one.</param>
		/// <remarks>The replacement function should determine whether or not the current node is the desired one, and then return one of the following:
		/// <list type="bullet">
		///     <item>The original node if it is not the desired node or the node is modified in-place.</item>
		///     <item>A single <see cref="IWikiNode"/> to replace the original node with. If this is a <see cref="NodeCollection"/>, the individual items of the collection will be inserted rather than the collection itself.</item>
		///     <item><see langword="null"/> if the node should be removed.</item>
		/// </list>
		/// </remarks>
		public void Replace(Func<IWikiNode, IWikiNode> replaceMethod)
		{
			ThrowNull(replaceMethod, nameof(replaceMethod));

			// Slower as a forward loop, but proceeds in the order a user would likely expect, and allows user to have matches fail after first/X replacement(s). Could be implimented as reverse or bidirectional, if needed.
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

				var newNode = replaceMethod(currentNode.Value);
				var nextNode = currentNode.Next;
				if (!ReferenceEquals(currentNode, newNode))
				{
					if (newNode != null)
					{
						if (newNode is NodeCollection newNodes)
						{
							foreach (var colNode in newNodes)
							{
								this.AddBefore(currentNode, colNode);
							}
						}
						else
						{
							this.AddBefore(currentNode, newNode);
						}
					}

					this.Remove(currentNode);
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

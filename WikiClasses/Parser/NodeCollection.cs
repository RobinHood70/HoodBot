namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	public class NodeCollection : WikiNode, IList<WikiNode>
	{
		#region Fields
		private readonly List<WikiNode> nodes = new List<WikiNode>();
		#endregion

		#region Constructors
		public NodeCollection()
		{
		}

		/// <summary>Initializes a new instance of the <see cref="NodeCollection"/> class.</summary>
		/// <param name="nodes">The nodes to initialize the collection with.</param>
		public NodeCollection(IEnumerable<WikiNode> nodes) => this.nodes.AddRange(nodes);

		/// <summary>Initializes a new instance of the <see cref="NodeCollection"/> class.</summary>
		/// <param name="nodes">The nodes to initialize the collection with.</param>
		public NodeCollection(params WikiNode[] nodes)
			: this(nodes as IEnumerable<WikiNode>)
		{
		}
		#endregion

		#region Public Properties
		public int Count => this.nodes.Count;

		bool ICollection<WikiNode>.IsReadOnly => false;
		#endregion

		#region Public Indexers
		public WikiNode this[int index]
		{
			get => this.nodes[index];
			set
			{
				ThrowNull(value, nameof(value));
				value.Parent = this;
				this.nodes[index] = value;
			}
		}
		#endregion

		#region Public Methods
		public override void Accept(INodeVisitor visitor) => visitor?.Visit(this);

		public void Add(WikiNode item)
		{
			ThrowNull(item, nameof(item));
			item.Parent = this;
			this.nodes.Add(item);
		}

		public void AddRange(IEnumerable<WikiNode> collection)
		{
			ThrowNull(collection, nameof(collection));
			var newCollection = new List<WikiNode>(collection);
			foreach (var node in newCollection)
			{
				node.Parent = this;
			}

			this.nodes.AddRange(collection);
		}

		public void Clear() => this.nodes.Clear();

		public bool Contains(WikiNode item) => this.nodes.Contains(item);

		public void CopyTo(WikiNode[] array, int arrayIndex) => this.nodes.CopyTo(array, arrayIndex);

		IEnumerator IEnumerable.GetEnumerator() => this.nodes.GetEnumerator();

		public IEnumerator<WikiNode> GetEnumerator() => this.nodes.GetEnumerator();

		public NodeCollection GetRange(int index, int count) => new NodeCollection(this.nodes.GetRange(index, count));

		public int IndexOf(WikiNode item) => this.nodes.IndexOf(item);

		public void Insert(int index, WikiNode item)
		{
			ThrowNull(item, nameof(item));
			item.Parent = this;
			this.nodes.Insert(index, item);
		}

		public bool Remove(WikiNode item) => this.nodes.Remove(item);

		/// <summary>Removes all nodes of the given type.</summary>
		/// <typeparam name="T">The type of node to remove.</typeparam>
		public void RemoveAll<T>()
			where T : WikiNode => this.Replace((node) => node is T ? null : node);

		/// <summary>Replaces the specified node with a new node.</summary>
		/// <param name="replaceMethod">A function to replace the value with a new one.</param>
		/// <remarks>The replacement function should determine whether or not the current node is the desired one, and then return one of the following:
		/// <list type="bullet">
		///     <item>The original node if it is not the desired node or the node is modified in-place.</item>
		///     <item>A single <see cref="WikiNode"/> to replace the original node with. If this is a <see cref="NodeCollection"/>, the individual items of the collection will be inserted rather than the collection itself.</item>
		///     <item><see langword="null"/> if the node should be removed.</item>
		/// </list>
		/// </remarks>
		public void Replace(Func<WikiNode, WikiNode> replaceMethod)
		{
			ThrowNull(replaceMethod, nameof(replaceMethod));

			// Slower as a forward loop, but proceeds in the order a user would likely expect, and allows user to have matches fail after first/X replacement(s). Could be implimented as reverse or bidirectional, if needed.
			var i = 0;
			while (i < this.nodes.Count)
			{
				var node = this.nodes[i];
				if (node is IEnumerable<NodeCollection> tree)
				{
					foreach (var subnode in tree)
					{
						subnode.Replace(replaceMethod);
					}
				}

				var newNode = replaceMethod(node);
				var iMove = 1;
				if (!ReferenceEquals(node, newNode))
				{
					if (newNode == null)
					{
						this.nodes.RemoveAt(i);
						iMove = 0;
					}
					else if (newNode is NodeCollection newNodes)
					{
						this.nodes.RemoveAt(i);
						this.nodes.InsertRange(i, newNodes);
						iMove = newNodes.Count;
					}
					else
					{
						this.nodes[i] = newNode;
					}
				}

				i += iMove;
			}
		}

		public void RemoveAt(int index) => this.nodes.RemoveAt(index);

		/// <summary>Replaces all nodes of the specified type with a single node inserted at the position of the first node of the specified type, or at the beginning of the collection if no nodes of the desired type are found.</summary>
		/// <typeparam name="T">The type of node to be removed. Constrained to <see cref="WikiNode"/>.</typeparam>
		/// <param name="newNode">The new node.</param>
		/// <remarks>This will commonly be used to replace text that may be interspersed with non-text by a single value. See the example.</remarks>
		/// <example>Using <c>MyTemplateNode.Title.ReplaceAll&lt;TextNode&gt;(new TextNode("NewName"));</c> on a template like <c>{{&lt;!--Leading comment-->Template&lt;!--Interior comment-->Name}}</c> would produce <c>{{&lt;!--Leading comment-->NewName}}</c>.</example>
		public void ReplaceAll<T>(WikiNode newNode)
			where T : WikiNode
		{
			var offset = -1;
			for (var i = 0; i < this.nodes.Count; i++)
			{
				if (this.nodes[i] is T)
				{
					offset = i;
					break;
				}
			}

			if (offset == -1)
			{
				this.nodes.Insert(0, newNode);
			}
			else
			{
				this.nodes[offset] = newNode;
				for (var i = this.nodes.Count - 1; i > offset; i--)
				{
					if (this.nodes[i] is T)
					{
						this.nodes.RemoveAt(i);
					}
				}
			}
		}
		#endregion
	}
}

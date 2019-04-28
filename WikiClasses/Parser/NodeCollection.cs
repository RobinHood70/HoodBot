namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;
	using RobinHood70.WikiClasses.Parser.Nodes;

	public class NodeCollection : IList<INodeBase>, INodeBase
	{
		#region Fields
		private readonly List<INodeBase> nodes;
		#endregion

		#region Constructors
		public NodeCollection() => this.nodes = new List<INodeBase>();

		public NodeCollection(IEnumerable<INodeBase> nodes) => this.nodes = new List<INodeBase>(nodes);
		#endregion

		#region Public Properties
		public int Count => this.nodes.Count;

		bool ICollection<INodeBase>.IsReadOnly => false;
		#endregion

		#region Public Indexers
		public INodeBase this[int index] { get => this.nodes[index]; set => this.nodes[index] = value; }
		#endregion

		#region Public Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);

		public void Add(INodeBase item) => this.nodes.Add(item);

		public void AddRange(IEnumerable<INodeBase> collection) => this.nodes.AddRange(collection);

		public void Clear() => this.nodes.Clear();

		public bool Contains(INodeBase item) => this.nodes.Contains(item);

		public void CopyTo(INodeBase[] array, int arrayIndex) => this.nodes.CopyTo(array, arrayIndex);

		IEnumerator IEnumerable.GetEnumerator() => this.nodes.GetEnumerator();

		public IEnumerator<INodeBase> GetEnumerator() => this.nodes.GetEnumerator();

		public NodeCollection GetRange(int index, int count) => new NodeCollection(this.nodes.GetRange(index, count));

		public int IndexOf(INodeBase item) => this.nodes.IndexOf(item);

		public void Insert(int index, INodeBase item) => this.nodes.Insert(index, item);

		public bool Remove(INodeBase item) => this.nodes.Remove(item);

		public void RemoveAt(int index) => this.nodes.RemoveAt(index);
		#endregion

		#region Internal Methods
		internal void AddLiteral(string text)
		{
			var last = this.nodes.Count - 1;
			if (last == -1)
			{
				this.Add(new TextNode(text));
			}
			else
			{
				if (!(this.nodes[last] is TextNode node))
				{
					this.Add(new TextNode(text));
				}
				else
				{
					node.Text += text;
				}
			}
		}

		internal void Merge(NodeCollection newList)
		{
			if (newList == null || newList.Count == 0)
			{
				return;
			}

			var startAt = 0;
			var last = this.nodes.Count - 1;
			if (last > -1)
			{
				if (this.nodes[last] is TextNode lastNode && newList[0] is TextNode first)
				{
					lastNode.Text += first.Text;
					startAt = 1;
				}
			}

			this.nodes.AddRange(startAt == 0 ? newList : newList.GetRange(startAt, newList.Count - startAt));
		}
		#endregion
	}
}

namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using System.Collections.Generic;

	internal class ElementNodeCollection : List<IWikiNode>
	{
		#region Constructors
		public ElementNodeCollection(params IWikiNode[] nodes)
			: base(nodes)
		{
		}
		#endregion

		#region Public Methods
		public void AddLiteral(string literal)
		{
			var last = this.Count - 1;
			if (last == -1)
			{
				this.Add(new TextNode(literal));
			}
			else
			{
				if (this[last] is TextNode node)
				{
					node.Text += literal;
				}
				else
				{
					this.Add(new TextNode(literal));
				}
			}
		}

		public void Merge(ElementNodeCollection newList)
		{
			if (newList.Count == 0)
			{
				return;
			}

			var merged = false;
			var last = this.Count - 1;
			if (last > -1)
			{
				if (this[last] is TextNode lastNode && newList[0] is TextNode first)
				{
					lastNode.Text += first.Text;
					merged = true;
				}
			}

			this.AddRange(merged ? newList.GetRange(1, newList.Count - 1) : newList);
		}
		#endregion
	}
}

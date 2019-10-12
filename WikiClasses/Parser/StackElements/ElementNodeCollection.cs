namespace RobinHood70.WikiClasses.Parser.StackElements
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
				if (!(this[last] is TextNode node))
				{
					this.Add(new TextNode(literal));
				}
				else
				{
					node.Text += literal;
				}
			}
		}

		public void Merge(ElementNodeCollection newList)
		{
			if (newList.Count == 0)
			{
				return;
			}

			var startAt = 0;
			var last = this.Count - 1;
			if (last > -1)
			{
				if (this[last] is TextNode lastNode && newList[0] is TextNode first)
				{
					lastNode.Text += first.Text;
					startAt = 1;
				}
			}

			this.AddRange(startAt == 0 ? newList : newList.GetRange(startAt, newList.Count - startAt));
		}
		#endregion
	}
}

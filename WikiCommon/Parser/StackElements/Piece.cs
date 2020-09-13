namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class Piece : List<IWikiNode>
	{
		#region Public Properties
		public int CommentEnd { get; set; } = -1;

		public int SplitPos { get; set; } = -1; // Not needed everywhere, but kind of silly to have two separate classes for the sake of a single property.

		public int VisualEnd { get; set; } = -1;
		#endregion

		#region Public Methods
		public void AddLiteral(IWikiNodeFactory factory, string literal)
		{
			if (this.Count == 0 || !(this[^1] is ITextNode node))
			{
				this.Add(factory.TextNode(literal));
			}
			else
			{
				node.Text += literal;
			}
		}

		public void Merge(List<IWikiNode> newList)
		{
			if (newList.Count == 0)
			{
				return;
			}

			var merged = false;
			var last = this.Count - 1;
			if (last > -1)
			{
				if (this[last] is ITextNode lastNode && newList[0] is ITextNode first)
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
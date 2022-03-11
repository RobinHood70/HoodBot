namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class Piece
	{
		#region Public Properties
		public int CommentEnd { get; set; } = -1;

		public List<IWikiNode> Nodes { get; } = new List<IWikiNode>();

		public int SplitPos { get; set; } = -1; // Not needed everywhere, but kind of silly to have two separate classes for the sake of a single property.

		public int VisualEnd { get; set; } = -1;
		#endregion

		#region Public Methods
		public void AddLiteral(IWikiNodeFactory factory, string literal)
		{
			if (this.Nodes.Count == 0 || this.Nodes[^1] is not ITextNode node)
			{
				this.Nodes.Add(factory.TextNode(literal));
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
			var last = this.Nodes.Count - 1;
			if (last > -1 && this.Nodes[last] is ITextNode lastNode && newList[0] is ITextNode first)
			{
				lastNode.Text += first.Text;
				merged = true;
			}

			this.Nodes.AddRange(merged ? newList.GetRange(1, newList.Count - 1) : newList);
		}
		#endregion
	}
}
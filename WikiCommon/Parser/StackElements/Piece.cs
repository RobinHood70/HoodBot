namespace RobinHood70.WikiCommon.Parser.StackElements;

using System.Collections.Generic;
using RobinHood70.WikiCommon.Parser;

internal class Piece
{
	#region Public Properties
	public List<IWikiNode> Nodes { get; } = [];
	#endregion

	#region Public Methods
	public void AddLiteral(IWikiNodeFactory factory, string literal)
	{
		if (this.Nodes.Count > 0 && this.Nodes[^1] is ITextNode node)
		{
			node.Text += literal;
		}
		else
		{
			this.Nodes.Add(factory.TextNode(literal));
		}
	}

	public void MergeText(List<IWikiNode> newList)
	{
		if (newList.Count > 0)
		{
			var merged = false;
			var last = this.Nodes.Count - 1;
			if (last > -1 && this.Nodes[last] is ITextNode lastNode && newList[0] is ITextNode first)
			{
				lastNode.Text += first.Text;
				merged = true;
			}

			this.Nodes.AddRange(merged ? newList.GetRange(1, newList.Count - 1) : newList);
		}
	}
	#endregion
}
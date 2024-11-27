namespace RobinHood70.WikiCommon.Parser.StackElements;

using System.Collections.Generic;

internal abstract class OpenCloseElement : StackElement
{
	#region Fields
	private readonly char open;
	private int length;
	#endregion

	#region Constructors
	protected OpenCloseElement(WikiStack stack, char open, int length)
		: base(stack)
	{
		this.open = open;
		this.length = length;
		this.DividerPieces.Add(new());
	}
	#endregion

	#region Internal Override Properties
	internal override DividerPiece CurrentPiece => this.DividerPieces[^1];
	#endregion

	#region Protected Properties

	protected List<DividerPiece> DividerPieces { get; } = [];
	#endregion

	#region Public Override Methods
	internal override List<IWikiNode> Backtrack() => this.Backtrack(this.length);
	#endregion

	#region Protected Methods
	protected List<IWikiNode> Backtrack(int matchingCount)
	{
		var newPiece = new Piece();
		newPiece.Nodes.Add(this.Stack.Factory.TextNode(new string(this.open, matchingCount)));
		var oldPiece = this.DividerPieces[0];
		newPiece.MergeText(oldPiece.Nodes);
		var pieceCount = this.DividerPieces.Count;
		for (var j = 1; j < pieceCount; j++)
		{
			oldPiece = this.DividerPieces[j];
			newPiece.AddLiteral(this.Stack.Factory, "|");
			newPiece.MergeText(oldPiece.Nodes);
		}

		return newPiece.Nodes;
	}

	protected int ParseClose(char found)
	{
		var count = this.Stack.Text.Span(found, this.Stack.Index, this.length);
		if (count < 2)
		{
			this.CurrentPiece.AddLiteral(this.Stack.Factory, new string(found, count));
			this.Stack.Index += count;
			return count;
		}

		List<IParameterNode> parameters = [];
		var pieceCount = this.DividerPieces.Count;
		var matchingCount = (found == ']' || count == 2) ? 2 : 3;
		var factory = this.Stack.Factory;
		for (var i = 1; i < pieceCount; i++)
		{
			var nvPiece = this.DividerPieces[i];
			List<IWikiNode>? name;
			List<IWikiNode> value;
			if (matchingCount != 3 && nvPiece is DividerPiece divider && divider.Position != -1)
			{
				name = divider.Nodes.GetRange(0, divider.Position);
				value = divider.Nodes.GetRange(divider.Position + 1, divider.Nodes.Count - divider.Position - 1);
			}
			else
			{
				name = null;
				value = nvPiece.Nodes;
			}

			parameters.Add(factory.ParameterNode(name, value));
		}

		var title = this.DividerPieces[0];
		IWikiNode node = found == ']'
			? factory.LinkNode(title.Nodes, parameters)
			: matchingCount == 3
				? factory.ArgumentNode(title.Nodes, parameters)
				: factory.TemplateNode(title.Nodes, parameters);
		this.Stack.Index += matchingCount;
		this.Stack.Pop();
		if (matchingCount < this.length)
		{
			this.DividerPieces.Clear();
			this.DividerPieces.Add(new());
			this.length -= matchingCount;
			if (this.length >= 2)
			{
				this.Stack.Push(this);
			}
			else
			{
				this.Stack.Top.CurrentPiece.Nodes.Add(this.Stack.Factory.TextNode(new string(this.open, this.length)));
			}
		}

		this.Stack.Top.CurrentPiece.Nodes.Add(node);
		return matchingCount;
	}
	#endregion
}
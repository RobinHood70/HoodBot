namespace RobinHood70.WikiCommon.Parser.StackElements;

using System.Collections.Generic;

internal abstract class OpenCloseElement : StackElement
{
	#region Constructors
	protected OpenCloseElement(WikiStack stack, char open, int length)
		: base(stack)
	{
		this.Open = open;
		this.Length = length;
		this.DividerPieces.Add(new());
	}
	#endregion

	#region Internal Override Properties
	internal override DividerPiece CurrentPiece => this.DividerPieces[^1];
	#endregion

	#region Protected Properties

	protected List<DividerPiece> DividerPieces { get; } = [];

	protected int Length { get; set; }

	protected char Open { get; }
	#endregion

	#region Public Override Methods
	internal override List<IWikiNode> Backtrack() => this.Backtrack(this.Length);
	#endregion

	#region Protected Methods
	protected List<IWikiNode> Backtrack(int matchingCount)
	{
		var newPiece = new Piece();
		newPiece.Nodes.Add(this.Stack.Factory.TextNode(new string(this.Open, matchingCount)));
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
	#endregion
}
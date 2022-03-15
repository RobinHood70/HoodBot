namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using RobinHood70.WikiCommon.Parser;

	internal sealed class TemplateElement : PairedElement
	{
		#region Fields
		// private readonly bool atLineStart;
		private int braceLength;
		#endregion

		#region Constructors
		public TemplateElement(WikiStack stack, int length)
			: base(stack, '{', length)
		{
			this.braceLength = length; // this.atLineStart = atLineStart;
		}
		#endregion

		#region Internal Override Properties
		internal override string SearchString => (this.PairedPieces[^1] is PairedPiece pp && pp.SplitPos == -1)
			? SearchBase + "|}="
			: SearchBase + "|}";
		#endregion

		#region Public Override Methods
		public override string ToString() => this.braceLength == 2 ? "template" : "argument";
		#endregion

		#region Internal Override Methods
		internal override void Parse(char found)
		{
			switch (found)
			{
				case '|':
					this.PairedPieces.Add(new());
					this.Stack.Index++;
					break;
				case '=':
					var lastPiece = this.PairedPieces[^1];
					lastPiece.SplitPos = lastPiece.Nodes.Count;
					if (this.PairedPieces.Count == 1)
					{
						lastPiece.AddLiteral(this.Stack.NodeFactory, "=");
					}
					else
					{
						// Node type isn't really relevant here, as long as it's not a TextNode. IgnoreNode made the most sense. This is an interim value that won't ever make it to the final output. This could probably be done with SplitPos alone, but adding this makes the TextNode checks in AddLiteral and Merge fail in their own right, without having to check SplitPos.
						lastPiece.Nodes.Add(this.Stack.NodeFactory.IgnoreNode("="));
					}

					this.Stack.Index++;
					break;
				case '}':
					var foundCount = this.ParseClose(found);
					if (foundCount > 1)
					{
						this.braceLength = foundCount;
					}

					break;
				default:
					this.Stack.ParseCharacter(found);
					break;
			}
		}
		#endregion
	}
}
namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using RobinHood70.WikiCommon.Parser;

	internal sealed class TemplateElement(WikiStack stack, int braceLength) : OpenCloseElement(stack, '{', braceLength)
	{
		#region Private Constants
		private const string SearchTitle = SearchBase + "|}";
		private const string SearchParam = SearchTitle + "=";
		#endregion

		#region Fields

		// Brace braceLength is stored separately from the parent class' braceLength because the parent modifies the value as it parses. This also allows the parent class' braceLength to be private.
		private int braceLength = braceLength;

		#endregion

		#region Internal Override Properties
		internal override string SearchString => (
			this.DividerPieces[^1] is DividerPiece pp &&
			pp.Position == -1)
				? SearchParam
				: SearchTitle;
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
					this.DividerPieces.Add(new());
					this.Stack.Index++;
					break;
				case '=':
					var lastDivider = this.DividerPieces[^1];
					lastDivider.Position = lastDivider.Nodes.Count;
					if (this.DividerPieces.Count == 1)
					{
						lastDivider.AddLiteral(this.Stack.NodeFactory, "=");
					}
					else
					{
						// Node type isn't really relevant here, as long as it's not a TextNode. IgnoreNode made the most sense. This is an interim value that won't ever make it to the final output. This could probably be done with SplitPos alone, but adding this makes the TextNode checks in AddLiteral and Merge fail in their own right, without having to check SplitPos.
						lastDivider.Nodes.Add(this.Stack.NodeFactory.IgnoreNode("="));
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
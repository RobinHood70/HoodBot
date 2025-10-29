namespace RobinHood70.WikiCommon.Parser.StackElements;
internal sealed class LinkElement(WikiStack stack, int length) : OpenCloseElement(stack, '[', length)
{
	#region Fields
	private bool dividerFound;
	#endregion

	#region Internal Override Properties
	internal override string SearchString => SearchBase + (this.dividerFound ? "]" : "|]");
	#endregion

	#region Public Override Methods
	public override string ToString() => "link";
	#endregion

	#region Internal Override Methods
	internal override void Parse(char found)
	{
		switch (found)
		{
			case '|':
				this.DividerPieces.Add(new());
				this.Stack.Index++;
				this.dividerFound = true;
				break;
			case ']':
				this.ParseClose();
				break;
			default:
				this.Stack.ParseCharacter(found);
				break;
		}
	}
	#endregion

	#region Private Methods
	private void ParseClose()
	{
		var count = this.Stack.Text.Span(']', this.Stack.Index, this.Length);
		if (count < 2)
		{
			this.CurrentPiece.AddLiteral(this.Stack.Factory, new string(']', count));
			this.Stack.Index += count;
			return;
		}

		var node = this.Stack.Factory.LinkNode(
			this.DividerPieces[0].Nodes,
			this.DividerPieces.Count == 2 ? this.DividerPieces[1].Nodes : []);
		this.Stack.Index += 2;
		this.Stack.Pop();
		if (this.Length > 2)
		{
			this.DividerPieces.Clear();
			this.DividerPieces.Add(new());
			this.Length -= 2;
			if (this.Length >= 2)
			{
				this.Stack.Push(this);
			}
			else
			{
				this.Stack.Top.CurrentPiece.Nodes.Add(this.Stack.Factory.TextNode(new string(this.Open, this.Length)));
			}
		}

		this.Stack.Top.CurrentPiece.Nodes.Add(node);
		#endregion
	}
}
namespace RobinHood70.WikiCommon.Parser.StackElements;

using System.Collections.Generic;
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
					lastDivider.AddLiteral(this.Stack.Factory, "=");
				}
				else
				{
					// Node type isn't really relevant here, as long as it's not a TextNode. IgnoreNode made the most sense. This is an interim value that won't ever make it to the final output. This could probably be done with SplitPos alone, but adding this makes the TextNode checks in AddLiteral and Merge fail in their own right, without having to check SplitPos.
					lastDivider.Nodes.Add(this.Stack.Factory.IgnoreNode("="));
				}

				this.Stack.Index++;
				break;
			case '}':
				var foundCount = this.ParseClose();
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

	#region Private Methods

	private int ParseClose()
	{
		var count = this.Stack.Text.Span('}', this.Stack.Index, this.Length);
		if (count < 2)
		{
			this.CurrentPiece.AddLiteral(this.Stack.Factory, new string('}', count));
			this.Stack.Index += count;
			return count;
		}

		var pieceCount = this.DividerPieces.Count;
		var matchingCount = count == 2 ? 2 : 3;
		var factory = this.Stack.Factory;
		List<IParameterNode> parameters = [];
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

		IWikiNode node = matchingCount == 3
			? factory.ArgumentNode(this.DividerPieces[0].Nodes, parameters)
			: factory.TemplateNode(this.DividerPieces[0].Nodes, parameters);

		this.Stack.Index += matchingCount;
		this.Stack.Pop();
		if (matchingCount < this.Length)
		{
			this.DividerPieces.Clear();
			this.DividerPieces.Add(new());
			this.Length -= matchingCount;
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
		return matchingCount;
	}
	#endregion
}
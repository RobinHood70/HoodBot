namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using System.Collections.Generic;

	internal abstract class PairedElement : StackElement
	{
		#region Fields
		private readonly char open;
		#endregion

		#region Constructors
		protected PairedElement(WikiStack stack, char open, int length)
			: base(stack)
		{
			this.open = open;
			this.Length = length;
			this.NameValuePieces.Add(new Piece());
		}
		#endregion

		#region Internal Override Properties
		internal override Piece CurrentPiece => this.NameValuePieces[^1];
		#endregion

		#region Protected Properties
		protected int Length { get; set; }

		protected List<Piece> NameValuePieces { get; } = new List<Piece>();
		#endregion

		#region Public Override Methods
		internal override List<IWikiNode> BreakSyntax() => this.BreakSyntax(this.Length);
		#endregion

		#region Protected Methods
		protected List<IWikiNode> BreakSyntax(int matchingCount)
		{
			var newPiece = new Piece { this.Stack.NodeFactory.TextNode(new string(this.open, matchingCount)) };
			var oldPiece = this.NameValuePieces[0];
			newPiece.Merge(oldPiece);
			var pieceCount = this.NameValuePieces.Count;
			for (var j = 1; j < pieceCount; j++)
			{
				oldPiece = this.NameValuePieces[j];
				newPiece.AddLiteral(this.Stack.NodeFactory, "|");
				newPiece.Merge(oldPiece);
			}

			return newPiece;
		}

		protected int ParseClose(char found)
		{
			var count = this.Stack.Text.Span(found, this.Stack.Index, this.Length);
			if (count < 2)
			{
				this.CurrentPiece.AddLiteral(this.Stack.NodeFactory, new string(found, count));
				this.Stack.Index += count;
				return count;
			}

			var parameters = new List<IParameterNode>();
			var pieceCount = this.NameValuePieces.Count;
			var matchingCount = (found == ']' || count == 2) ? 2 : 3;
			var factory = this.Stack.NodeFactory;
			for (var i = 1; i < pieceCount; i++)
			{
				var nvPiece = this.NameValuePieces[i];
				parameters.Add(nvPiece.SplitPos == -1 || matchingCount == 3
					? factory.ParameterNode(null, nvPiece)
					: factory.ParameterNode(
						nvPiece.GetRange(0, nvPiece.SplitPos),
						nvPiece.GetRange(nvPiece.SplitPos + 1, nvPiece.Count - nvPiece.SplitPos - 1)));
			}

			var title = this.NameValuePieces[0];
			var node =
				matchingCount == 3 ? factory.ArgumentNode(title, parameters) :
				found == ']' ? factory.LinkNode(title, parameters) :
				factory.TemplateNode(title, parameters) as IWikiNode;
			this.Stack.Index += matchingCount;
			this.Stack.Pop();
			if (matchingCount < this.Length)
			{
				this.NameValuePieces.Clear();
				this.NameValuePieces.Add(new Piece());
				this.Length -= matchingCount;
				if (this.Length >= 2)
				{
					this.Stack.Push(this);
				}
				else
				{
					this.Stack.Top.CurrentPiece.Add(this.Stack.NodeFactory.TextNode(new string(this.open, this.Length)));
				}
			}

			this.Stack.Top.CurrentPiece.Add(node);
			return matchingCount;
		}
		#endregion
	}
}

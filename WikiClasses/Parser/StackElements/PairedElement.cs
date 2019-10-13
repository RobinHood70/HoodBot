namespace RobinHood70.WikiClasses.Parser.StackElements
{
	using System.Collections.Generic;

	internal abstract class PairedElement : StackElement
	{
		#region Fields
		private readonly char open;
		#endregion

		#region Constructors
		public PairedElement(WikiStack stack, char open, int length)
			: base(stack)
		{
			this.open = open;
			this.Length = length;
			this.NameValuePieces.Add(new Piece());
		}
		#endregion

		#region Internal Override Properties
		internal override Piece CurrentPiece => this.NameValuePieces[this.NameValuePieces.Count - 1];
		#endregion

		#region Protected Properties
		protected int Length { get; set; }

		protected List<Piece> NameValuePieces { get; } = new List<Piece>();
		#endregion

		#region Public Override Methods
		internal override ElementNodeCollection BreakSyntax() => this.BreakSyntax(this.Length);
		#endregion

		#region Protected Methods
		protected ElementNodeCollection BreakSyntax(int matchingCount)
		{
			var nodes = new ElementNodeCollection(new TextNode(new string(this.open, matchingCount)));
			var piece = this.NameValuePieces[0];
			nodes.Merge(piece);
			var pieceCount = this.NameValuePieces.Count;
			for (var j = 1; j < pieceCount; j++)
			{
				piece = this.NameValuePieces[j];
				nodes.AddLiteral("|");
				nodes.Merge(piece);
			}

			return nodes;
		}

		protected int ParseClose(char found)
		{
			var count = this.Stack.Text.Span(found, this.Stack.Index, this.Length);
			if (count < 2)
			{
				this.CurrentPiece.AddLiteral(new string(found, count));
				this.Stack.Index += count;
				return count;
			}

			var parameters = new List<ParameterNode>();
			var argIndex = 1;
			var pieceCount = this.NameValuePieces.Count;
			var matchingCount = (found == ']' || count == 2) ? 2 : 3;
			for (var i = 1; i < pieceCount; i++)
			{
				var nvPiece = this.NameValuePieces[i];
				parameters.Add(nvPiece.SplitPos == -1 || matchingCount == 3
					? new ParameterNode(argIndex++, nvPiece)
					: new ParameterNode(nvPiece.GetRange(0, nvPiece.SplitPos), nvPiece.GetRange(nvPiece.SplitPos + 1, nvPiece.Count - nvPiece.SplitPos - 1)));
			}

			var node =
				matchingCount == 3 ? new ArgumentNode(this.NameValuePieces[0], parameters) :
				found == ']' ? new LinkNode(this.NameValuePieces[0], parameters) :
				new TemplateNode(this.NameValuePieces[0], parameters) as IWikiNode;
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
					this.Stack.Top.CurrentPiece.Add(new TextNode(new string(this.open, this.Length)));
				}
			}

			this.Stack.Top.CurrentPiece.Add(node);
			return matchingCount;
		}
		#endregion
	}
}

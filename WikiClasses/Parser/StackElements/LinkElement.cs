namespace RobinHood70.WikiClasses.Parser.StackElements
{
	using System.Collections.Generic;

	internal class LinkElement : PairedElement
	{
		#region Constructors
		public LinkElement(WikiStack stack, int length)
			: base(stack, '[', length)
		{
		}
		#endregion

		#region Internal Override Properties
		internal override string SearchString => SearchBase + "|]";
		#endregion

		#region Public Override Methods
		public override string ToString() => "link";
		#endregion

		#region Internal Override Methods
		internal override void Parse(char found)
		{
			var stack = this.Stack;
			switch (found)
			{
				case '|':
					this.NameValuePieces.Add(new Piece());
					stack.Index++;
					break;
				case ']':
					var count = stack.Text.Span(']', stack.Index, this.Length);
					if (count < 2)
					{
						this.CurrentPiece.AddLiteral(new string(']', count));
						stack.Index += count;
						return;
					}

					var parameters = new List<ParameterNode>();
					var argIndex = 1;
					var pieceCount = this.NameValuePieces.Count;
					for (var i = 1; i < pieceCount; i++)
					{
						var nvPiece = this.NameValuePieces[i];
						parameters.Add(nvPiece.SplitPos == -1
							? new ParameterNode(argIndex++, nvPiece)
							: new ParameterNode(nvPiece.GetRange(0, nvPiece.SplitPos), nvPiece.GetRange(nvPiece.SplitPos + 1, nvPiece.Count - nvPiece.SplitPos - 1)));
					}

					this.ParseClose(2);
					var link = new LinkNode(this.NameValuePieces[0], parameters);
					stack.Top.CurrentPiece.Add(link);
					break;
				default:
					stack.Parse(found);
					break;
			}
		}
		#endregion
	}
}

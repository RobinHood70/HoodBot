namespace RobinHood70.WikiClasses.Parser.StackElements
{
	using System.Collections.Generic;

	internal class TemplateElement : PairedElement
	{
		#region Fields
		// private readonly bool atLineStart;
		private int braceLength;
		#endregion

		#region Constructors
		public TemplateElement(WikiStack stack, int length)
			: base(stack, '{', length) => this.braceLength = length; // this.atLineStart = atLineStart;
		#endregion

		#region Internal Override Properties
		internal override string SearchString => this.NameValuePieces[this.NameValuePieces.Count - 1].SplitPos == -1
			? SearchBase + "|}="
			: SearchBase + "|}";
		#endregion

		#region Public Override Methods
		public override string ToString() => this.braceLength == 2 ? "template" : "argument";
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
				case '=':
					var lastPiece = this.NameValuePieces[this.NameValuePieces.Count - 1];
					lastPiece.SplitPos = lastPiece.Count;
					lastPiece.Add(new EqualsNode());
					stack.Index++;
					break;
				case '}':
					var count = stack.Text.Span('}', stack.Index, this.Length);
					if (count < 2)
					{
						this.CurrentPiece.AddLiteral(new string('}', count));
						stack.Index += count;
						return;
					}

					this.braceLength = count;
					var matchingCount = count == 2 ? 2 : 3;
					var parameters = new List<ParameterNode>();
					var argIndex = 1;
					for (var i = 1; i < this.NameValuePieces.Count; i++)
					{
						var nvPiece = this.NameValuePieces[i];
						parameters.Add(nvPiece.SplitPos == -1
							? new ParameterNode(argIndex++, nvPiece.ToNodeCollection())
							: new ParameterNode(nvPiece.ToNodeCollection(0, nvPiece.SplitPos), nvPiece.ToNodeCollection(nvPiece.SplitPos + 1, nvPiece.Count - nvPiece.SplitPos - 1)));
					}

					this.ParseClose(matchingCount);
					if (matchingCount == 2)
					{
						stack.Top.CurrentPiece.Add(new TemplateNode(this.Length == matchingCount && this.atLineStart, this.NameValuePieces[0].ToNodeCollection(), parameters));
					}
					else
					{
						stack.Top.CurrentPiece.Add(new ArgumentNode(this.Length == matchingCount && this.atLineStart, this.NameValuePieces[0].ToNodeCollection(), parameters));
					}
					break;
				default:
					stack.Parse(found);
					break;
			}
		}
		#endregion
	}
}
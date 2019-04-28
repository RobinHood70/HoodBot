namespace RobinHood70.WikiClasses.Parser.StackElements
{
	using System.Collections.Generic;
	using RobinHood70.WikiClasses.Parser.Nodes;

	internal class TemplateElement : PairedElement
	{
		#region Fields
		private readonly bool atLineStart;
		private TemplateNodeType type;
		#endregion

		#region Constructors
		public TemplateElement(WikiStack stack, int length, bool atLineStart)
			: base(stack, '{', length)
		{
			this.atLineStart = atLineStart;
			this.type = length == 2 ? TemplateNodeType.Template : TemplateNodeType.Argument; // This is a guess for debugging purposes until } is found, at which point this will be updated to an accurate value.
		}
		#endregion

		#region Internal Override Properties
		internal override string SearchString => this.NameValuePieces[this.NameValuePieces.Count - 1].SplitPos == -1
			? SearchBase + "|}="
			: SearchBase + "|}";
		#endregion

		#region Public Override Methods
		public override string ToString() => this.type.ToString();
		#endregion

		#region Internal Override Methods
		internal override void Parse(char found)
		{
			var stack = this.Stack;
			switch (found)
			{
				case '|':
					this.NameValuePieces.Add(new NameValuePiece());
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

					this.type = count == 2 ? TemplateNodeType.Template : TemplateNodeType.Argument;
					var matchingCount = count == 2 ? 2 : 3;
					var parameters = new List<ParameterNode>();
					var argIndex = 1;
					var partCount = this.NameValuePieces.Count;
					for (var i = 1; i < partCount; i++)
					{
						var nvPiece = this.NameValuePieces[i];
						parameters.Add(nvPiece.SplitPos == -1
							? new ParameterNode(argIndex++, nvPiece)
							: new ParameterNode(nvPiece.GetRange(0, nvPiece.SplitPos), nvPiece.GetRange(nvPiece.SplitPos + 1, nvPiece.Count - nvPiece.SplitPos - 1)));
					}

					this.ParseClose(matchingCount);
					var template = new TemplateNode(this.type, this.Length == matchingCount && this.atLineStart, this.NameValuePieces[0], parameters);
					stack.Top.CurrentPiece.Add(template);
					break;
				default:
					stack.Parse(found);
					break;
			}
		}
		#endregion
	}
}

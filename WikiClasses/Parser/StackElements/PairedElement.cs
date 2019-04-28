namespace RobinHood70.WikiClasses.Parser.StackElements
{
	using System.Collections.Generic;
	using RobinHood70.WikiClasses.Parser.Nodes;

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
			this.NameValuePieces.Add(new NameValuePiece());
		}
		#endregion

		#region Internal Override Properties
		internal override Piece CurrentPiece => this.NameValuePieces[this.NameValuePieces.Count - 1];
		#endregion

		#region Protected Properties
		protected int Length { get; set; }

		protected List<NameValuePiece> NameValuePieces { get; } = new List<NameValuePiece>();
		#endregion

		#region Public Override Methods
		internal override NodeCollection BreakSyntax() => this.BreakSyntax(this.Length);
		#endregion

		#region Protected Methods
		protected NodeCollection BreakSyntax(int matchingCount)
		{
			var nodes = new NodeCollection
			{
				new TextNode(new string(this.open, matchingCount))
			};

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

		protected void ParseClose(int matchingCount)
		{
			var stack = this.Stack;
			stack.Index += matchingCount;
			stack.Pop();
			if (matchingCount < this.Length)
			{
				this.NameValuePieces.Clear();
				this.NameValuePieces.Add(new NameValuePiece());
				this.Length -= matchingCount;
				if (this.Length >= 2)
				{
					stack.Push(this);
				}
				else
				{
					stack.Top.CurrentPiece.Add(new TextNode(new string(this.open, this.Length)));
				}
			}
		}
		#endregion
	}
}

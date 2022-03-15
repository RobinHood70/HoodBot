namespace RobinHood70.WikiCommon.Parser.StackElements
{
	internal sealed class HeaderPiece : Piece
	{
		public int CommentEnd { get; set; } = -1;

		public int VisualEnd { get; set; } = -1;

	}
}

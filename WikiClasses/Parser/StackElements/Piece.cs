namespace RobinHood70.WikiClasses.Parser.StackElements
{
	internal class Piece : ElementNodeCollection
	{
		#region Public Properties
		public int CommentEnd { get; set; } = -1;

		public int VisualEnd { get; set; } = -1;
		#endregion
	}
}
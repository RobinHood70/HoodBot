namespace RobinHood70.WikiCommon.Parser.StackElements
{
	internal class Piece : ElementNodeCollection
	{
		#region Public Properties
		public int CommentEnd { get; set; } = -1;

		public int SplitPos { get; set; } = -1; // Not needed everywhere, but kind of silly to have two separate classes for the sake of a single property.

		public int VisualEnd { get; set; } = -1;
		#endregion
	}
}
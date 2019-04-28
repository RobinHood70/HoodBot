namespace RobinHood70.WikiClasses.Parser
{
	public class Piece : NodeCollection
	{
		#region Public Properties
		public int CommentEnd { get; set; } = -1;

		public int VisualEnd { get; set; } = -1;
		#endregion
	}
}
namespace RobinHood70.WikiClasses.Parser.StackElements
{
	internal abstract class StackElement
	{
		#region Protected Constants
		protected const string SearchBase = "[{<\n";
		#endregion

		#region Constructors
		internal StackElement(WikiStack stack) => this.Stack = stack;
		#endregion

		#region Internal Abstract Properties
		internal abstract Piece CurrentPiece { get; }

		internal abstract string SearchString { get; }
		#endregion

		#region Protected Properties
		protected WikiStack Stack { get; }
		#endregion

		#region Public Abstract Methods
		public abstract override string ToString();
		#endregion

		#region Internal Abstract Methods
		internal abstract NodeCollection BreakSyntax();

		internal abstract void Parse(char found);
		#endregion
	}
}

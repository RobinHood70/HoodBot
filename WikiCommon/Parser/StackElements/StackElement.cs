namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using System.Collections.Generic;

	internal abstract class StackElement
	{
		#region Protected Constants
		protected const string SearchBase = "[{<\n";
		#endregion

		#region Constructors
		protected StackElement(WikiStack stack)
		{
			this.Stack = stack;
		}
		#endregion

		#region Internal Abstract Properties
		internal abstract Piece CurrentPiece { get; }

		#endregion

		#region Internal Virtual Properties
		internal virtual string SearchString { get; } = SearchBase;
		#endregion

		#region Protected Properties
		protected WikiStack Stack { get; }
		#endregion

		#region Public Abstract Methods
		public abstract override string ToString();
		#endregion

		#region Internal Abstract Methods
		internal abstract List<IWikiNode> Backtrack();

		internal abstract void Parse(char found);
		#endregion
	}
}
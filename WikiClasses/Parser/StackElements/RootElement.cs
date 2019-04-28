namespace RobinHood70.WikiClasses.Parser.StackElements
{
	using System;

	internal class RootElement : StackElement
	{
		#region Constructors
		internal RootElement(WikiStack stack)
			: base(stack)
		{
		}
		#endregion

		#region Internal Override Properties
		internal override Piece CurrentPiece { get; } = new Piece();

		internal override string SearchString => SearchBase;
		#endregion

		#region Public Override Methods
		public override string ToString() => "root";
		#endregion

		#region Internal Override Methods
		internal override NodeCollection BreakSyntax() => throw new InvalidOperationException("BreakSyntax should never be called on the Root Stack Element.");

		internal override void Parse(char found) => this.Stack.Parse(found);
		#endregion
	}
}

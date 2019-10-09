namespace RobinHood70.WikiClasses.Parser.StackElements
{
	using System;
	using RobinHood70.WikiClasses.Properties;
	using static WikiCommon.Globals;

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
		internal override ElementNodeCollection BreakSyntax() => throw new InvalidOperationException(CurrentCulture(Resources.CalledOnRoot, nameof(this.BreakSyntax)));

		internal override void Parse(char found) => this.Stack.Parse(found);
		#endregion
	}
}

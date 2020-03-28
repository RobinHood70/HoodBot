namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.WikiCommon.Properties;
	using static CommonCode.Globals;

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
		[DoesNotReturn]
		internal override ElementNodeCollection BreakSyntax() => throw new InvalidOperationException(CurrentCulture(Resources.CalledOnRoot, nameof(this.BreakSyntax)));

		internal override void Parse(char found) => this.Stack.Parse(found);
		#endregion
	}
}

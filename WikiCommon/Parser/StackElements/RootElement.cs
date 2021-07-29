namespace RobinHood70.WikiCommon.Parser.StackElements
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Properties;

	internal sealed class RootElement : StackElement
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
		internal override List<IWikiNode> BreakSyntax() => throw new InvalidOperationException(Globals.CurrentCulture(Resources.CalledOnRoot, nameof(this.BreakSyntax)));

		internal override void Parse(char found) => this.Stack.ParseCharacter(found);
		#endregion
	}
}

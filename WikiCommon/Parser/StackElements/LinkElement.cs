﻿namespace RobinHood70.WikiCommon.Parser.StackElements;

internal sealed class LinkElement(WikiStack stack, int length) : OpenCloseElement(stack, '[', length)
{
	#region Internal Override Properties
	internal override string SearchString { get; } = SearchBase + "|]";
	#endregion

	#region Public Override Methods
	public override string ToString() => "link";
	#endregion

	#region Internal Override Methods
	internal override void Parse(char found)
	{
		switch (found)
		{
			case '|':
				this.DividerPieces.Add(new());
				this.Stack.Index++;
				break;
			case ']':
				this.ParseClose(found);
				break;
			default:
				this.Stack.ParseCharacter(found);
				break;
		}
	}
	#endregion
}
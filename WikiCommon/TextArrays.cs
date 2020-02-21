namespace RobinHood70.WikiCommon
{
	using System;
	using System.Diagnostics.CodeAnalysis;

	/// <summary>A static class of split values used throughout all related projects. This avoids frequent re-allocations of small array values—particularly important inside loops and frequently-called methods.</summary>
	public static class TextArrays
	{
#pragma warning disable CS1591 // These should all be self-documenting.
		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "Elements could be altered, theoretically, but this is unlikely.")]
		public static readonly char[] At = { '@' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] CategorySeparators = { ' ', '-' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Colon = { ':' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Comma = { ',' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly string[] CommaSpace = { ", " };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly string[] EnvironmentNewLine = { Environment.NewLine };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] EqualsSign = { '=' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] LineFeed = { '\n' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly string[] LinkTerminator = { "[[" };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly string[] LinkMarker = { "[[" };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] NewLineChars = { '\n', '\r' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Octothorp = { '#' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Parentheses = { '(', ')' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Period = { '.' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Pipe = { '|' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Plus = { '+' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Semicolon = { ';' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Slash = { '/' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] SquareBrackets = { '[', ']' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly char[] Tab = { '\t' };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly string[] TemplateTerminator = { "{{" };

		[SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly", Justification = "As above.")]
		public static readonly string[] TemplateMarker = { "{{" };
#pragma warning restore CS1591
	}
}

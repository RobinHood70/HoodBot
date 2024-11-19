﻿namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;

	/// <summary>Groups all magic word/template parts into a single <see langword="object"/>.</summary>
	/// <param name="Name">The name of the magic word/template. This should be strictly the part that comes before the colon, if any.</param>
	/// <param name="FirstArgument">For parser functions, the part that comes after the initial colon and before the first pipe.</param>
	/// <param name="Parameters">All pipe-separated parameters. Anonymous parameters should be included in the dictionary as consecutively numbered parameters.</param>
	/// <param name="Parent">The parent TemplateContext object.</param>
	public record TemplateStack(string Name, string? FirstArgument, IDictionary<string, string> Parameters, TemplateStack? Parent)
	{
		public int Depth { get; } = Parent is null ? 0 : Parent.Depth + 1;

		public static TemplateStack NewRoot() => new(string.Empty, null, new Dictionary<string, string>(StringComparer.Ordinal), null);
	}
}

namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	/// <summary>Stores information about the current magic word/template as well as tracking all of its parents.</summary>
	public class MagicWordFrame
	{
		/// <summary>Initializes a new instance of the <see cref="MagicWordFrame"/> class.</summary>
		/// <param name="text">The name portion of the magic word/template, including the first parameter for parser functions.</param>
		/// <param name="parameters">The parameters of the magic word/template.</param>
		/// <param name="parent">The parent frame.</param>
		public MagicWordFrame(string text, IDictionary<string, string> parameters, MagicWordFrame? parent)
		{
			ArgumentNullException.ThrowIfNull(text);
			ArgumentNullException.ThrowIfNull(parameters);
			var split = text.Split(TextArrays.Colon, 2);
			this.Name = split[0];
			this.FirstArgument = split.Length == 2 ? split[1] : null;
			this.Parameters = parameters;
			this.Parent = parent;
			this.Depth = parent is null ? 0 : parent.Depth + 1;
		}

		#region Public Properties

		/// <summary>Gets the number of <see cref="MagicWordFrame"/>s above this one.</summary>
		public int Depth { get; }

		/// <summary>Gets the parameter that comes after the initial colon for parser functions.</summary>
		public string? FirstArgument { get; }

		/// <summary>Gets a value indicating whether the curent magic word is a parser function.</summary>
		public bool IsParserFunction => this.FirstArgument is not null;

		/// <summary>Gets a value indicating whether the curent magic word could be a variable.</summary>
		/// <remarks>Parameterless templates can look identical to variables, hence the uncertainty in the name.</remarks>
		public bool MayBeVariable => this.FirstArgument is null && this.Parameters.Count == 0;

		/// <summary>Gets the name of the magic word/template. This should be strictly the part that comes before the colon, if any.</summary>
		public string Name { get; }

		/// <summary>Gets the parameter list. Anonymous parameters should be included in the dictionary as numbered parameters with text names.</summary>
		public IDictionary<string, string> Parameters { get; }

		/// <summary>Gets the parent <see cref="MagicWordFrame"/>.</summary>
		public MagicWordFrame? Parent { get; }
		#endregion

		#region Public Methods

		/// <summary>Creates a new root <see cref="MagicWordFrame"/>.</summary>
		/// <returns>A new <see cref="MagicWordFrame"/> with all empty parameters.</returns>
		/// <remarks>The dictionary is initialized as normal rather than using an ImmutableDictionary.Empty to allow future implementation of magic words like MetaTemplate's {{#local}} and similar that can define pseudo-parameters at the root level.</remarks>
		public static MagicWordFrame CreateRoot() => new(string.Empty, new Dictionary<string, string>(StringComparer.Ordinal), null);
		#endregion
	}
}

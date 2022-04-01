namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	/// <summary>Represents a parameter with collection of values, normally separated by pipe characters.</summary>
	/// <seealso cref="Parameter" />
	public class PipedParameter : Parameter
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PipedParameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values. Any duplicates in the input will be ignored.</param>
		public PipedParameter(string name, ICollection<string> values)
			: base(name)
		{
			this.Values = values.NotNull();
		}
		#endregion

		#region Public Abstract Properties

		/// <summary>Gets the collection of values.</summary>
		public ICollection<string> Values { get; }
		#endregion

		#region Public Methods

		/// <summary>Builds a pipe-separated string from an enumerable parameter.</summary>
		/// <param name="supportsUnitSeparator">if set to <see langword="true"/>, 0x1F is supported as an alternative to pipes, when needed.</param>
		/// <returns>A pipe-separated string with all the values from the enumeration.</returns>
		public string BuildPipedValue(bool supportsUnitSeparator)
		{
			const char altSep = '\x1f';
			var lead = string.Empty;
			if (supportsUnitSeparator)
			{
				foreach (var item in this.Values)
				{
					if (item.Contains('|', StringComparison.Ordinal))
					{
						lead = new string(altSep, 1);
						break;
					}
				}
			}

			// We used to append an extra pipe to the end if the result ended in =, pipe, or alt-pipe, or if this.Values was empty, but this doesn't seem to be necessary, and could lead to unexpected errors for otherwise valid input. Might be necessary under certain conditions or for certain MW versions, though, so restore that if needed.
			var sep = lead.Length == 0 ? '|' : altSep;
			return lead + string.Join(sep, this.Values);
		}
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override void Accept(IParameterVisitor visitor) => visitor.NotNull().Visit(this);

		/// <inheritdoc/>
		public override string ToString() => this.Name + "=" + string.Join("|", this.Values);
		#endregion
	}
}

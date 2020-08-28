namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Represents a parameter with collection of values, normally separated by pipe characters.</summary>
	public abstract class MultiValuedParameter : Parameter
	{
		#region Contructors

		/// <summary>Initializes a new instance of the <see cref="MultiValuedParameter"/> class.</summary>
		/// <param name="name">The parameter name.</param>
		protected MultiValuedParameter(string name)
			: base(name)
		{
		}
		#endregion

		#region Public Abstract Properties

		/// <summary>Gets the collection of values.</summary>
		public abstract IEnumerable<string> Values { get; }
		#endregion

		/// <summary>Builds a pipe-separated string from an enumerable parameter.</summary>
		/// <param name="supportsUnitSeparator">if set to <see langword="true"/>, 0x1F is supported as an alternative to pipes, when needed.</param>
		public void BuildPipedValue(StringBuilder builder, bool supportsUnitSeparator)
		{
		}

		/// <summary>Builds a pipe-separated string from an enumerable parameter.</summary>
		/// <param name="supportsUnitSeparator">if set to <see langword="true"/>, 0x1F is supported as an alternative to pipes, when needed.</param>
		/// <returns>A pipe-separated string with all the values from the enumeration.</returns>
		public string BuildPipedValue(bool supportsUnitSeparator)
		{
			var lead = string.Empty;
			var separator = '|';
			if (supportsUnitSeparator)
			{
				foreach (var item in this.Values)
				{
					if (item.Contains('|', StringComparison.Ordinal))
					{
						separator = '\x1f';
						lead += separator; // If using alternate separator, we have to flag this by emitting a leading separator.
						break;
					}
				}
			}

			// We used to append an extra pipe to the end if the result ended in =, pipe, or alt-pipe, or if this.Values was empty, but this doesn't seem to be necessary, and could lead to unexpected errors for otherwise valid input. Might be necessary under certain conditions or for certain MW versions, though, so restore that if needed.
			return lead + string.Join(separator, this.Values);
		}
	}
}

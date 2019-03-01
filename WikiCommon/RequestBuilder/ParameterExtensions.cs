namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System.Collections.Generic;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Extensions to the parameter classes.</summary>
	public static class ParameterExtensions
	{
		/// <summary>Builds a pipe-separated string from an enumerable parameter.</summary>
		/// <typeparam name="T">Any <see cref="IEnumerable{T}"/> of type <see cref="string"/>, such as from <see cref="PipedListParameter"/> or <see cref="PipedParameter"/>.</typeparam>
		/// <param name="parameter">The parameter.</param>
		/// <param name="supportsUnitSeparator">if set to <c>true</c>, 0x1F is supported as an alternative to pipes, when needed.</param>
		/// <returns>A pipe-separated string with all the values from the enumeration.</returns>
		public static string BuildPipedValue<T>(this Parameter<T> parameter, bool supportsUnitSeparator)
			where T : IEnumerable<string>
		{
			ThrowNull(parameter, nameof(parameter));
			string value = null;
			if (supportsUnitSeparator)
			{
				// Although this could be done with the existing builder, it gets a bit messy with Uri encoding then checking for the pipe afterwards, so use a separate builder like other similar classes.
				var sb = new StringBuilder();
				foreach (var item in parameter.Value)
				{
					sb.Append(item.Contains("|") ? '\x1f' + item + '\x1f' : '|' + item);
				}

				if (sb.Length > 0 && sb[0] == '|')
				{
					sb.Remove(0, 1);
				}

				value = sb.ToString();
			}
			else
			{
				value = string.Join("|", parameter.Value);
			}

			if (value.Length == 0)
			{
				return "|";
			}

			var last = value[value.Length - 1];
			if (last == '|' || last == '=' || last == '\x1f')
			{
				value += '|';
			}

			return value;
		}
	}
}

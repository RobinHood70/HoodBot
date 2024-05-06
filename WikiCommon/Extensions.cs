namespace RobinHood70.WikiCommon
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Text;

	/// <summary>Extension methods for a variety of types.</summary>
	public static class Extensions
	{
		#region DateTime Extensions

		/// <summary>Formats a <see cref="DateTime"/> in the standard MediaWiki format.</summary>
		/// <param name="timestamp">The timestamp to format.</param>
		/// <returns>A string with the date in the standard MediaWiki format.</returns>
		public static string ToMediaWiki(this DateTime timestamp) => timestamp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK", CultureInfo.InvariantCulture);

		/// <summary>Formats a <see cref="DateTime"/>? in the standard MediaWiki format.</summary>
		/// <param name="timestamp">The timestamp to format.</param>
		/// <returns>A string with the date in the standard MediaWiki format or <see langword="null"/> if the input value was null.</returns>
		[return: NotNullIfNotNull(nameof(timestamp))]
		public static string? ToMediaWiki(this DateTime? timestamp) => timestamp == null ? null : ToMediaWiki(timestamp.Value);
		#endregion

		#region StringBuilderExtensions

		/// <summary>Identical to <see cref="StringBuilder.AppendLine()"/>, but only appends an LF, not a full CRLF.</summary>
		/// <param name="sb">The sb.</param>
		/// <returns>The current StringBuilder.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="sb"/> is null.</exception>
		public static StringBuilder AppendLinefeed(this StringBuilder sb)
		{
			ArgumentNullException.ThrowIfNull(sb);
			return sb.Append('\n');
		}

		/// <summary>Identical to <see cref="StringBuilder.AppendLine(string?)"/>, but only appends an LF, not a full CRLF.</summary>
		/// <param name="sb">The sb.</param>
		/// <param name="value">The text to append before the linefeed.</param>
		/// <returns>The current StringBuilder.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="sb"/> is null.</exception>
		public static StringBuilder AppendLinefeed(this StringBuilder sb, string value)
		{
			ArgumentNullException.ThrowIfNull(sb);
			return sb
				.Append(value)
				.Append('\n');
		}
		#endregion
	}
}
namespace RobinHood70.WikiClasses.Parser
{
	using System.Globalization;
	using static WikiCommon.Globals;

	/// <summary>Class Extensions.</summary>
	public static class Extensions
	{
		#region String Extensions

		/// <summary>Limits text to the specified maximum length.</summary>
		/// <param name="text">The text.</param>
		/// <param name="maxLength">The maximum length.</param>
		/// <returns>System.String.</returns>
		/// <remarks>This limits only the initial string length, not the total, so the return value can have a maximum length of maxLength + 3.</remarks>
		public static string Ellipsis(this string text, int maxLength) =>
			text == null ? null :
			text.Length > maxLength ? text.Substring(0, maxLength) + "..." :
			text;

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string text, char mask, int offset) => Span(text, mask.ToString(), offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string text, char mask, int offset, int limit) => Span(text, mask.ToString(), offset, limit);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string text, string mask, int offset) => Span(text, mask, offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string text, string mask, int offset, int limit)
		{
			ThrowNull(text, nameof(text));
			ThrowNull(mask, nameof(mask));
			if (offset < 0)
			{
				offset += text.Length;
			}

			if (offset < 0 || offset >= text.Length)
			{
				return 0;
			}

			if (limit < 0)
			{
				limit += text.Length;
				if (limit < 0)
				{
					return 0;
				}
			}
			else
			{
				limit += offset;
			}

			if (limit > text.Length)
			{
				limit = text.Length;
			}

			var baseOffset = offset;
			while (offset < limit && mask.IndexOf(text[offset]) != -1)
			{
				offset++;
			}

			return offset - baseOffset;
		}

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string text, char mask, int offset) => SpanReverse(text, mask.ToString(), offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string text, char mask, int offset, int limit) => SpanReverse(text, mask.ToString(), offset, limit);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string text, string mask, int offset) => SpanReverse(text, mask, offset, text?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="text">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string text, string mask, int offset, int limit)
		{
			ThrowNull(text, nameof(text));
			ThrowNull(mask, nameof(mask));
			if (offset < 0)
			{
				offset += text.Length;
			}

			if (offset <= 0 || offset > text.Length)
			{
				return 0;
			}

			limit = limit < 0 ? -limit : offset - limit;
			if (limit < 0)
			{
				limit = 0;
			}
			else if (limit >= offset)
			{
				return 0;
			}

			// Decrement offset because we're going in the reverse direction, so want to look at the character *before* the current one.
			offset--;
			var baseOffset = offset;
			while (offset >= limit && mask.IndexOf(text[offset]) != -1)
			{
				offset--;
			}

			return baseOffset - offset;
		}

		/// <summary>Equivalent to calling <see cref="int.ToString()"/> with the invariant culture.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The input integer converted to an invariant string.</returns>
		public static string ToStringInvariant(this int value) => value.ToString(CultureInfo.InvariantCulture);
		#endregion
	}
}
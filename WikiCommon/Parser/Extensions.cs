namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Globalization;
	using static CommonCode.Globals;

	/// <summary>Class Extensions.</summary>
	public static class Extensions
	{
		#region String Extensions

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string txt, char mask, int offset) => Span(txt, new string(mask, 1), offset, txt?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string txt, char mask, int offset, int limit) => Span(txt, new string(mask, 1), offset, limit);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string txt, string mask, int offset) => Span(txt, mask, offset, txt?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int Span(this string txt, string mask, int offset, int limit)
		{
			ThrowNull(txt, nameof(txt));
			ThrowNull(mask, nameof(mask));
			if (offset < 0)
			{
				offset += txt.Length;
			}

			if (offset < 0 || offset >= txt.Length)
			{
				return 0;
			}

			if (limit < 0)
			{
				limit += txt.Length;
				if (limit < 0)
				{
					return 0;
				}
			}
			else
			{
				limit += offset;
			}

			if (limit > txt.Length)
			{
				limit = txt.Length;
			}

			var baseOffset = offset;
			while (offset < limit && mask.IndexOf(txt[offset], StringComparison.Ordinal) != -1)
			{
				offset++;
			}

			return offset - baseOffset;
		}

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string txt, char mask, int offset) => SpanReverse(txt, new string(mask, 1), offset, txt?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching character to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string txt, char mask, int offset, int limit) => SpanReverse(txt, new string(mask, 1), offset, limit);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string txt, string mask, int offset) => SpanReverse(txt, mask, offset, txt?.Length ?? 0);

		/// <summary>Counts the number of consecutive characters that match the mask character.</summary>
		/// <param name="txt">The text to scan.</param>
		/// <param name="mask">The matching characters to count.</param>
		/// <param name="offset">The zero-based offset into the string to start scanning from.</param>
		/// <param name="limit">The maximum number of matching characters allowed.</param>
		/// <returns>The matching character count.</returns>
		public static int SpanReverse(this string txt, string mask, int offset, int limit)
		{
			ThrowNull(txt, nameof(txt));
			ThrowNull(mask, nameof(mask));
			if (offset < 0)
			{
				offset += txt.Length;
			}

			if (offset <= 0 || offset > txt.Length)
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
			while (offset >= limit && mask.IndexOf(txt[offset], StringComparison.Ordinal) != -1)
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
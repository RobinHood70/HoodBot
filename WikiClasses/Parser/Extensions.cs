namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Globalization;
	using static WikiCommon.Globals;

	public static class Extensions
	{
		#region String Extensions
		public static int Span(this string text, char mask) => Span(text, mask.ToString(), 0, text?.Length ?? 0);

		public static int Span(this string text, char mask, int offset) => Span(text, mask.ToString(), offset, text?.Length ?? 0);

		public static int Span(this string text, char mask, int offset, int limit) => Span(text, mask.ToString(), offset, limit);

		public static int Span(this string text, string mask) => Span(text, mask, 0, text?.Length ?? 0);

		public static int Span(this string text, string mask, int offset) => Span(text, mask, offset, text?.Length ?? 0);

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

		public static int SpanReverse(this string text, char mask) => SpanReverse(text, mask.ToString(), text?.Length ?? 0, text?.Length ?? 0);

		public static int SpanReverse(this string text, char mask, int offset) => SpanReverse(text, mask.ToString(), offset, text?.Length ?? 0);

		public static int SpanReverse(this string text, char mask, int offset, int length) => SpanReverse(text, mask.ToString(), offset, length);

		public static int SpanReverse(this string text, string mask) => SpanReverse(text, mask, text?.Length ?? 0, text?.Length ?? 0);

		public static int SpanReverse(this string text, string mask, int offset) => SpanReverse(text, mask, offset, text?.Length ?? 0);

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

		public static string ToStringInvariant(this int value) => value.ToString(CultureInfo.InvariantCulture);
		#endregion
	}
}
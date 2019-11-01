namespace RobinHood70.WikiCommon
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using static RobinHood70.WikiCommon.Globals;

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
		public static string? ToMediaWiki(this DateTime? timestamp) => timestamp == null ? null : ToMediaWiki(timestamp.Value);
		#endregion

		#region Enum Extensions

		/// <summary>Gets each single-bit value of a flags enumeration.</summary>
		/// <typeparam name="T">The enumeration type.</typeparam>
		/// <param name="flagValue">The flags enumeration value to enumerate.</param>
		/// <returns>An enumeration of every single-bit value in the specified flags enumeration.</returns>
		public static IEnumerable<T> GetUniqueFlags<T>(this T flagValue)
			where T : Enum
		{
			ulong flag = 1;
			foreach (T value in Enum.GetValues(flagValue.GetType()))
			{
				var bits = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
				while (flag < bits)
				{
					flag <<= 1;
				}

				if (flag == bits && flagValue.HasFlag(value))
				{
					yield return value;
				}
			}
		}

		/// <summary>Determines whether or not an enum represents a single-bit flag value.</summary>
		/// <param name="flagValue">The flags enumeration value to check.</param>
		/// <returns>True if the flag value represents a single-bit value.</returns>
		public static bool IsUniqueFlag(this Enum flagValue)
		{
			var numericFlags = Convert.ToUInt64(flagValue, CultureInfo.InvariantCulture);
			return (numericFlags & (numericFlags - 1)) == 0 && numericFlags != 0;
		}
		#endregion

		#region ICollection<T> Extensions

		/// <summary>Adds one collection of items to another.</summary>
		/// <typeparam name="T">The type of items.</typeparam>
		/// <param name="collection">The original collection.</param>
		/// <param name="values">The values to be added.</param>
		public static void AddRange<T>(this ICollection<T> collection, params T[] values) => AddRange(collection, values as IEnumerable<T>);

		/// <summary>Adds one collection of items to another.</summary>
		/// <typeparam name="T">The type of items.</typeparam>
		/// <param name="collection">The original collection.</param>
		/// <param name="values">The collection to be added.</param>
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> values)
		{
			ThrowNull(collection, nameof(collection));
			if (values != null)
			{
				if (collection is List<T> list)
				{
					list.AddRange(values);
				}
				else
				{
					using var enumerator = values.GetEnumerator();
					while (enumerator.MoveNext())
					{
						collection.Add(enumerator.Current);
					}
				}
			}
		}
		#endregion

		#region IDictionary<TKey, TValue> Extensions

		/// <summary>Convenience method to convert a dictionary to read-only.</summary>
		/// <typeparam name="TKey">The key-type of the <paramref name="dictionary" /> (inferred).</typeparam>
		/// <typeparam name="TValue">The value-type of the <paramref name="dictionary" /> (inferred).</typeparam>
		/// <param name="dictionary">The dictionary to convert.</param>
		/// <returns>A read-only dictionary based on the provided dictionary. If the input was null, an empty read-only dictionary is returned.</returns>
		public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue>? dictionary) => new ReadOnlyDictionary<TKey, TValue>(dictionary ?? new Dictionary<TKey, TValue>());

		/// <summary>Gets the value of the "first" item in a dictionary.</summary>
		/// <typeparam name="TKey">The key type of the dictionary.</typeparam>
		/// <typeparam name="TValue">The value type of the dictionary.</typeparam>
		/// <param name="dictionary">The dictionary from which to retrieve the first value.</param>
		/// <returns>The first value in the enumerable, or throws an error.</returns>
		/// <exception cref="KeyNotFoundException">The list was empty.</exception>
		/// <remarks>Although the current implementation of dictionaries appears to maintain insertion order, this is not guaranteed. This function should be used only to get the value from a single-entry dictionary, or to get a unspecified value from a multi-entry dictionary.</remarks>
		public static TValue First<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
		{
			ThrowNull(dictionary, nameof(dictionary));
			using var enumerator = dictionary.GetEnumerator();
			return enumerator.MoveNext() ? enumerator.Current.Value : throw new InvalidOperationException();
		}
		#endregion

		#region IEnumerable<T> Extensions

		/// <summary>Casts the enumerable to an IReadOnlyList if possible, or creates a new one if needed.</summary>
		/// <typeparam name="T">The type of the original enumerable.</typeparam>
		/// <param name="enumerable">The enumerable to convert.</param>
		/// <returns>The existing enumerable as an IReadOnlyList or a new list.</returns>
		public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T>? enumerable) => enumerable == null || IsEmpty(enumerable) ? Array.Empty<T>() : enumerable as IReadOnlyList<T> ?? new List<T>(enumerable);

		/// <summary>Determines whether an IEnumerable<typeparamref name="T"/> contains the specified value.</summary>
		/// <typeparam name="T">The type of the original enumerable.</typeparam>
		/// <param name="enumerable">The enumerable to convert.</param>
		/// <param name="value">The value to find.</param>
		/// <returns><see langword="true"/> if the collection contains the specified value; otherwise, <see langword="false"/>.</returns>
		public static bool Contains<T>(this IEnumerable<T> enumerable, T value) => (enumerable as ICollection<T>)?.Contains(value) ?? Contains(enumerable, value, null);

		/// <summary>Determines whether an IEnumerable<typeparamref name="T"/> contains the specified value.</summary>
		/// <typeparam name="T">The type of the original enumerable.</typeparam>
		/// <param name="enumerable">The enumerable to convert.</param>
		/// <param name="value">The value to find.</param>
		/// <param name="comparer">The equality comparer to use to make the comparison.</param>
		/// <returns><see langword="true"/> if the collection contains the specified value; otherwise, <see langword="false"/>.</returns>
		public static bool Contains<T>(this IEnumerable<T> enumerable, T value, IEqualityComparer<T>? comparer)
		{
			// It's understandable why this wasn't in IEnumerable<T>, since enumerating can potentially be a slow-running operation, but it's beyond me why this wasn't put into IReadOnlyCollection<T>. MS covered it with Linq, using a virtually identical implementation to this, but that's still only a workaround (as is this).
			ThrowNull(enumerable, nameof(enumerable));
			comparer ??= EqualityComparer<T>.Default;
			foreach (var item in enumerable)
			{
				if (comparer.Equals(item, value))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Gets the first item of the collection, or the specified default value.</summary>
		/// <typeparam name="T">The collection type.</typeparam>
		/// <param name="enumerable">The collection to enumerate.</param>
		/// <returns>The first item in the collection or the specified default value.</returns>
		[return: MaybeNull]
		public static T First<T>(this IEnumerable<T>? enumerable) => First(enumerable, default!);

		/// <summary>Gets the first item of the collection, or the specified default value.</summary>
		/// <typeparam name="T">The collection type.</typeparam>
		/// <param name="enumerable">The collection to enumerate.</param>
		/// <param name="defaultValue">The default value to use if the collection is empty.</param>
		/// <returns>The first item in the collection or the specified default value.</returns>
		[return: MaybeNull]
		public static T First<T>(this IEnumerable<T>? enumerable, [MaybeNull] T defaultValue)
		{
			if (enumerable != null)
			{
				using var enumerator = enumerable.GetEnumerator();
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}

			return defaultValue;
		}
		#endregion

		#region IEnumerable Extensions

		/// <summary>Determines whether an IEnumerable has items.</summary>
		/// <param name="enumerable">The enumerable to check.</param>
		/// <returns><see langword="true"/> if the list is non-null and has at least one item; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="KeyNotFoundException">The list was empty.</exception>
		public static bool IsEmpty(this IEnumerable? enumerable) => enumerable == null ? true : !enumerable.GetEnumerator().MoveNext();
		#endregion

		#region IFormattable Extensions

		// This was originally implemented as a generic. Why? I have a feeling it might've been something to do with unexpected name resolution at the time, but results seem consistent at this point.

		/// <summary>Convenience method to format any IFormattable value as an invariant value.</summary>
		/// <param name="value">The value to format.</param>
		/// <returns>The value as an invariant string.</returns>
		public static string? ToStringInvariant(this IFormattable value) => value?.ToString(null, CultureInfo.InvariantCulture);
		#endregion

		#region IList<T> Extensions

		/// <summary>Shuffles the specified list into a random order.</summary>
		/// <typeparam name="T">The list type.</typeparam>
		/// <param name="list">The list.</param>
		public static void Shuffle<T>(this IList<T>? list)
		{
			ThrowNull(list, nameof(list));
			var random = new Random();
			var i = list.Count - 1;
			if (i == 0)
			{
				return;
			}

			while (i >= 0)
			{
				var swapWith = random.Next(list.Count);
				var value = list[i];
				list[i] = list[swapWith];
				list[swapWith] = value;
				i--;
			}
		}
		#endregion

		#region String Extensions

		/// <summary>Limits text to the specified maximum length.</summary>
		/// <param name="text">The text.</param>
		/// <param name="maxLength">The maximum length.</param>
		/// <returns>System.String.</returns>
		/// <remarks>This limits only the initial string length, not the total, so the return value can have a maximum length of maxLength + 3.</remarks>
		public static string? Ellipsis(this string? text, int maxLength) =>
			text == null ? null :
			text.Length > maxLength ? text.Substring(0, maxLength) + "..." :
			text;

		/// <summary>Converts the first character of a string to upper-case.</summary>
		/// <param name="text">The string to alter.</param>
		/// <returns>A copy of the original string, with the first charcter converted to upper-case.</returns>
		public static string LowerFirst(this string text) => LowerFirst(text, CultureInfo.InvariantCulture);

		/// <summary>Converts the first character of a string to upper-case.</summary>
		/// <param name="text">The string to alter.</param>
		/// <param name="culture">The culture to use for converting the first character to upper-case.</param>
		/// <returns>A copy of the original string, with the first charcter converted to upper-case.</returns>
		public static string LowerFirst(this string text, CultureInfo culture)
		{
			ThrowNull(culture, nameof(culture));
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			var retval = text.Substring(0, 1).ToLower(culture);
			return text.Length == 1 ? retval : retval + text.Substring(1);
		}

		/// <summary>Converts the first character of a string to upper-case.</summary>
		/// <param name="text">The string to alter.</param>
		/// <returns>A copy of the original string, with the first charcter converted to upper-case.</returns>
		public static string UpperFirst(this string text) => UpperFirst(text, CultureInfo.InvariantCulture);

		/// <summary>Converts the first character of a string to upper-case.</summary>
		/// <param name="text">The string to alter.</param>
		/// <param name="culture">The culture to use for converting the first character to upper-case.</param>
		/// <returns>A copy of the original string, with the first charcter converted to upper-case.</returns>
		public static string UpperFirst(this string text, CultureInfo culture)
		{
			ThrowNull(culture, nameof(culture));
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			var retval = text.Substring(0, 1).ToUpper(culture);
			return text.Length == 1 ? retval : retval + text.Substring(1);
		}
		#endregion

#if DEBUG
		// Any calls to any of these methods should be replaced by native methods/properties.
		#region Honeypot Methods
		internal static void AddRange<T>(this List<T> list, params T[] values)
		{
			ThrowNull(list, nameof(list));
			ThrowNull(values, nameof(values));
			list.AddRange(values);
		}

		internal static IReadOnlyList<T> AsReadOnlyList<T>(this List<T>? list) => list?.AsReadOnly() ?? Array.Empty<T>() as IReadOnlyList<T>;

		[return: MaybeNull]
		internal static T First<T>(this IReadOnlyList<T> list) => list == null ? default : list[0];

		[return: MaybeNull]
		internal static T First<T>(this IReadOnlyList<T> list, [AllowNull] T defaultValue) => list == null ? defaultValue : list[0];

		internal static bool IsEmpty(this ICollection? collection) => collection == null || collection.Count == 0;
		#endregion
#endif
	}
}
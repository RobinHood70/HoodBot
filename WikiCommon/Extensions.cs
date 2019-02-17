namespace RobinHood70.WikiCommon
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Extension methods for a variety of types.</summary>
	public static class Extensions
	{
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
		/// <param name="original">The original collection.</param>
		/// <param name="collection">The collection to be added.</param>
		public static void AddRange<T>(this ICollection<T> original, IEnumerable<T> collection)
		{
			ThrowNull(original, nameof(original));
			if (collection != null)
			{
				if (original is List<T> list)
				{
					list.AddRange(collection);
				}
				else
				{
					using (var enumerator = collection.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							original.Add(enumerator.Current);
						}
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
		public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => new ReadOnlyDictionary<TKey, TValue>(dictionary ?? new Dictionary<TKey, TValue>());

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
			using (var enumerator = dictionary.GetEnumerator())
			{
				return enumerator.MoveNext() ? enumerator.Current.Value : throw new InvalidOperationException();
			}
		}
		#endregion

		#region IEnumerable<T> Extensions

		/// <summary>Casts the enumerable to an IReadOnlyCollection if possible, or creates a new one if needed.</summary>
		/// <typeparam name="T">The type of the original enumerable.</typeparam>
		/// <param name="list">The enumerable to convert.</param>
		/// <returns>The existing enumerable as an IReadOnlyCollection or a new list.</returns>
		public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> list) => list.HasItems() ? list as IReadOnlyCollection<T> ?? new List<T>(list) : Array.Empty<T>();

		/// <summary>Casts the enumerable to an IReadOnlyList if possible, or creates a new one if needed.</summary>
		/// <typeparam name="T">The type of the original enumerable.</typeparam>
		/// <param name="list">The enumerable to convert.</param>
		/// <returns>The existing enumerable as an IReadOnlyList or a new list.</returns>
		public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> list) => list.HasItems() ? list as IReadOnlyList<T> ?? new List<T>(list) : Array.Empty<T>();

		/// <summary>Gets the first item in an enumeration without adding a stupid amount of other Linq things that are really REALLY annoying at design time.</summary>
		/// <typeparam name="T">The type of the enumerable.</typeparam>
		/// <param name="list">The enumerable from which to retrieve the first value.</param>
		/// <returns>The first value in the enumerable, or throws an error.</returns>
		/// <exception cref="KeyNotFoundException">The list was empty.</exception>
		public static T First<T>(this IEnumerable<T> list)
		{
			ThrowNull(list, nameof(list));
			using (var enumerator = list.GetEnumerator())
			{
				return enumerator.MoveNext() ? enumerator.Current : throw new KeyNotFoundException();
			}
		}
		#endregion

		#region IEnumerable Extensions

		/// <summary>Nearly identical to Linq's Cast method, but limited to classes for better performance.</summary>
		/// <typeparam name="T">The type to convert to.</typeparam>
		/// <param name="collection">The collection to cast.</param>
		/// <returns>An enumeration with all items cast to the specified type.</returns>
		/// <remarks>This version of Cast requires that all return types be classes. It's most useful for collections like CookieCollection or MatchCollection, where the <see cref="IReadOnlyList{T}"/> interface was never implemented.</remarks>
		public static IEnumerable<T> Cast<T>(this IEnumerable collection)
			where T : class
		{
			foreach (var item in collection)
			{
				yield return item as T;
			}
		}

		/// <summary>Determines whether an IEnumerable has items.</summary>
		/// <param name="list">The enumerable to check.</param>
		/// <returns><c>true</c> if the list is non-null and has at least one item; otherwise, <c>false</c>.</returns>
		/// <exception cref="KeyNotFoundException">The list was empty.</exception>
		public static bool HasItems(this IEnumerable list) => list?.GetEnumerator().MoveNext() ?? false;
		#endregion

		#region IFormattable Extensions

		/// <summary>Convenience method to format any IFormattable value as an invariant value.</summary>
		/// <typeparam name="T">Any IFormattable.</typeparam>
		/// <param name="value">The value to format.</param>
		/// <returns>The value as an invariant string.</returns>
		public static string ToStringInvariant<T>(this T value)
			where T : IFormattable => value.ToString(null, CultureInfo.InvariantCulture);
		#endregion

		#region String Extensions

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
			if (text.Length > 1)
			{
				retval += text.Substring(1);
			}

			return retval;
		}
		#endregion
	}
}
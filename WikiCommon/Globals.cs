﻿namespace RobinHood70.WikiCommon
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;

	#region Public Delegates

	/// <summary>A strongly typed event handler delegate.</summary>
	/// <typeparam name="TSender">The type of the sender.</typeparam>
	/// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
	/// <param name="sender">The sender.</param>
	/// <param name="eventArgs">The event data.</param>
	// From: http://stackoverflow.com/questions/1046016/event-signature-in-net-using-a-strong-typed-sender and http://msdn.microsoft.com/en-us/library/sx2bwtw7.aspx. Originally had a TEventArgs : EventArgs constraint, but mirroring EventHandler<TEventArgs>, I removed it.
	[Serializable]
	public delegate void StrongEventHandler<TSender, TEventArgs>(TSender sender, TEventArgs eventArgs);
	#endregion

	/// <summary>Global helper methods that are useful in a variety of scenarios.</summary>
	public static class Globals
	{
		#region Public Properties

		/// <summary>Gets a value indicating whether the current project is using <a href="http://www.mono-project.com/">Mono</a>.</summary>
		public static bool HasMono { get; } = Type.GetType("Mono.Runtime") != null;

		/// <summary>Gets a value indicating whether the current project is running on Windows.</summary>
		public static bool OnWindows { get; } = Environment.OSVersion.Platform < PlatformID.Unix;
		#endregion

		#region Public Methods

		/// <summary>Generates a generic hash code based on multiple input hash codes.</summary>
		/// <param name="hashCodes">Hash codes from constitutent types.</param>
		/// <returns>An integer that is likely to be a good hash code for the combined values.</returns>
		public static int CompositeHashCode(params int[] hashCodes)
		{
			ThrowNull(hashCodes, nameof(hashCodes));
			unchecked
			{
				var hash = -2128831035;
				for (var i = 0; i < hashCodes.Length; i++)
				{
					hash = (hash * 16777219) ^ hashCodes[i];
				}

				return hash;
			}
		}

		/// <summary>Convenience method so that CurrentCulture and Invariant are all in the same class for both traditional and formattable strings, and are used the same way.</summary>
		/// <param name="text">The text to format.</param>
		/// <param name="values">The values of any parameters in the <paramref name="text" /> parameter.</param>
		/// <returns>The formatted text.</returns>
		public static string CurrentCulture(string text, params object[] values) => string.Format(CultureInfo.CurrentCulture, text, values);

		/// <summary>Convenience method so that CurrentCulture and Invariant are all in the same class for both traditional and formattable strings, and are used the same way.</summary>
		/// <param name="formattable">A formattable string.</param>
		/// <returns>The formatted text.</returns>
		public static string CurrentCulture(FormattableString formattable) => formattable?.ToString(CultureInfo.CurrentCulture);

		/// <summary>Creates an empty read-only dictionary of the specified type.</summary>
		/// <typeparam name="TKey">The key type.</typeparam>
		/// <typeparam name="TValue">The value type.</typeparam>
		/// <returns>An empty read-only dictionary.</returns>
		public static IReadOnlyDictionary<TKey, TValue> EmptyReadOnlyDictionary<TKey, TValue>() => new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());

		/// <summary>Converts paired show/hide flag enumerations to a Filter object.</summary>
		/// <param name="showOnly">Show only these types of entries.</param>
		/// <param name="hide">Hide these types of entries.</param>
		/// <param name="flag">Type of entry to convert to a Filter.</param>
		/// <returns>A Filter object that's set to Any if not set in either enum, Exclude if set in the hide enum (or both), and Only if set in the showOnly enum.</returns>
		public static Filter FlagToFilter(Enum showOnly, Enum hide, Enum flag)
		{
			ThrowNull(showOnly, nameof(showOnly));
			ThrowNull(hide, nameof(hide));
			ThrowNull(flag, nameof(flag));
			return hide.HasFlag(flag) ? Filter.Exclude :
				showOnly.HasFlag(flag) ? Filter.Only :
				Filter.Any;
		}

		/// <summary>Attempts to figure out the culture associated with the language code, falling back progressively through parent languages.</summary>
		/// <param name="languageCode">The language code to try.</param>
		/// <returns>The nearest CultureInfo possible to the given <paramref name="languageCode"/> or null if nothing is found.</returns>
		public static CultureInfo GetCulture(string languageCode)
		{
			// Try to figure out wiki culture; otherwise revert to CurrentCulture.
			if (!string.IsNullOrWhiteSpace(languageCode))
			{
				do
				{
					try
					{
						return new CultureInfo(languageCode);
					}
					catch (CultureNotFoundException)
					{
						var lastDash = languageCode.LastIndexOf('-');
						if (lastDash > -1)
						{
							languageCode = languageCode.Substring(0, lastDash);
						}
					}
				}
				while (languageCode.Length > 0);
			}

			return null;
		}

		/// <summary>Convenience method so that CurrentCulture and Invariant are all in the same class for both traditional and formattable strings, and are used the same way.</summary>
		/// <param name="invariantText">The text to format.</param>
		/// <param name="values">The values of any parameters in the <paramref name="invariantText" /> parameter.</param>
		/// <returns>The formatted text.</returns>
		/// <remarks>Note that the name of the input parameter is changed so rules will not identify it as needing localization, since invariant strings generally don't.</remarks>
		public static string Invariant(string invariantText, params object[] values) => string.Format(CultureInfo.InvariantCulture, invariantText, values);

		/// <summary>Convenience method so that CurrentCulture and Invariant are all in the same class for both traditional and formattable strings, and are used the same way.</summary>
		/// <param name="formattable">A formattable string.</param>
		/// <returns>The formatted text.</returns>
		// Copy of the same-named method from the FormattableString code so that all culture methods are in the same library.
		public static string Invariant(FormattableString formattable) => formattable?.ToString(CultureInfo.InvariantCulture);

		/// <summary>Normalizes the name of the message using the same process MediaWiki uses internally.</summary>
		/// <param name="name">The name.</param>
		/// <param name="culture">The culture to use to convert the first letter from upper-case to lower-case, if needed.</param>
		/// <returns>The input name with spaces converted to underscores and the first character converted to lower-case.</returns>
		public static string NormalizeMessageName(string name, CultureInfo culture)
		{
			ThrowNull(culture, nameof(culture));
			if (!string.IsNullOrEmpty(name))
			{
				name = name.Replace(' ', '_');

				// We have no knowledge of the wiki's culture, and doing so is complex and would probably be somewhat unreliable, so guess by using current culture.
				if (char.IsUpper(name[0]))
				{
					name = name.Length == 1 ? culture.TextInfo.ToLower(name[0]).ToString() : culture.TextInfo.ToLower(name[0]) + name.Substring(1);
				}
			}

			return name;
		}

		/// <summary>Throws an exception if the input value is null.</summary>
		/// <param name="nullable">The value that may be null.</param>
		/// <param name="name">The name of the parameter in the original method.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="nullable" /> is null.</exception>
		public static void ThrowNull([ValidatedNotNull] object nullable, string name)
		{
			if (nullable == null)
			{
				throw new ArgumentNullException(name);
			}
		}

		/// <summary>This reverses the process of <see cref="NormalizeMessageName(string, CultureInfo)"/>, making a message name look more like the page name.</summary>
		/// <param name="name">The name to convert.</param>
		/// <param name="culture">The culture to use to convert the first letter from lower-case to upper-case, if needed.</param>
		/// <returns>The input name with underscores converted to spaces and the first character converted to upper-case.</returns>
		public static string UnnormalizeMessageName(string name, CultureInfo culture)
		{
			ThrowNull(culture, nameof(culture));
			if (!string.IsNullOrEmpty(name))
			{
				name = name.Replace('_', ' ');

				// We have no knowledge of the wiki's culture, and doing so is complex and would probably be somewhat unreliable, so guess by using current culture.
				if (char.IsLower(name[0]))
				{
					name = name.Length == 1 ? culture.TextInfo.ToUpper(name[0]).ToString() : culture.TextInfo.ToUpper(name[0]) + name.Substring(1);
				}
			}

			return name;
		}
		#endregion
	}
}
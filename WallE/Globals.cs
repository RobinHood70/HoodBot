namespace RobinHood70.WallE
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Security.Cryptography;
	using System.Text;
	using RobinHood70.WallE.RequestBuilder;
	using static Properties.EveMessages;
	using static RobinHood70.Globals;

	#region Public Enumerations

	/// <summary>Represents a tristate filter, where the options are to show everything, show only the selected items, or hide the selected items.</summary>
	/// <remarks>This is, in effect, an alias for Tristate, but is much clearer in its intent than having True/False/Unknown values.</remarks>
	public enum FilterOption
	{
		/// <summary>No filter.</summary>
		All = Tristate.Unknown,

		/// <summary>Only include these results (e.g., redirects only).</summary>
		Only = Tristate.True,

		/// <summary>Filter out these results (e.g., non-redirects only).</summary>
		Filter = Tristate.False,
	}

	/// <summary>Represents a binary value which also allows for an unknown state.</summary>
	/// <remarks>This is used in preference to a <see cref="Nullable{Boolean}"/> due to the fact that it is both smaller and makes the code much clearer.</remarks>
	public enum Tristate
	{
		/// <summary>An unknown or unassigned value.</summary>
		Unknown = 0,

		/// <summary>The value is True.</summary>
		True,

		/// <summary>The value is false.</summary>
		False
	}
	#endregion

	internal static class Globals
	{
		#region Public Methods
		public static string BuildPipedValue<T>(Parameter<T> parameter, bool supportsUnitSeparator)
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "I am")]
		public static string GetHash(byte[] data, HashType hashType)
		{
			string retval = null;
			HashAlgorithm hash = null;

			try
			{
				switch (hashType)
				{
					case HashType.Md5:
						hash = MD5.Create();
						break;
					case HashType.Sha1:
						hash = SHA1.Create();
						break;
				}

				var sb = new StringBuilder(40);
				var hashBytes = hash.ComputeHash(data);
				foreach (var b in hashBytes)
				{
					sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
				}

				retval = sb.ToString();
			}
			finally
			{
				if (!HasMono)
				{
					hash?.Dispose();
				}
			}

			return retval;
		}

		public static string GetHash(this string data, HashType hashType) => GetHash(Encoding.UTF8.GetBytes(data ?? string.Empty), hashType);

		public static void ThrowNullCollection<T>(IEnumerable<T> collection, string paramName)
		{
			if (collection == null)
			{
				throw new ArgumentNullException(paramName);
			}

			using (var enumerator = collection.GetEnumerator())
			{
				if (!enumerator.MoveNext())
				{
					throw new ArgumentException(CurrentCulture(CollectionInvalid, paramName));
				}
			}
		}

		public static void ThrowNullRefCollection<T>(IEnumerable<T> collection, string paramName)
			where T : class
		{
			ThrowNullCollection(collection, paramName);
			foreach (var item in collection)
			{
				if (item == null)
				{
					throw new ArgumentException(CurrentCulture(CollectionInvalid, paramName));
				}
			}
		}

		public static void ThrowNullOrWhiteSpace(IEnumerable<string> collection, string paramName)
		{
			if (collection == null)
			{
				throw new ArgumentNullException(paramName);
			}

			foreach (var item in collection)
			{
				if (string.IsNullOrWhiteSpace(item))
				{
					throw new ArgumentException(CurrentCulture(CollectionInvalid, paramName));
				}
			}
		}

		public static void ThrowNullOrWhiteSpace(string text, string paramName)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				throw new ArgumentException(CurrentCulture(StringInvalid, paramName));
			}
		}
		#endregion
	}
}
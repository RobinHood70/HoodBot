namespace RobinHood70.WallE
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Security.Cryptography;
	using System.Text;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	#region Internal Enumerations
	internal enum HashType
	{
		Md5,
		Sha1
	}
	#endregion

	internal static class ProjectGlobals
	{
		#region Public Methods

		/// <summary>Creates an empty read-only dictionary of the specified type.</summary>
		/// <typeparam name="TKey">The key type.</typeparam>
		/// <typeparam name="TValue">The value type.</typeparam>
		/// <returns>An empty read-only dictionary.</returns>
		public static IReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary<TKey, TValue>() => new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());

		public static string GetHash(byte[] data, HashType hashType)
		{
			// At one point, this was a try/finally block because mono wasn't quite implementing IDisposable properly. This doesn't appear to be the case anymore, so doing it the traditional method. It looks like a using block would probably have worked in any event, since that would presumably coerce hash to IDisposable.
			// See https://xamarin.github.io/bugzilla-archives/33/3375/bug.html
			using var hash = hashType switch
			{
				HashType.Md5 => MD5.Create(),
				HashType.Sha1 => SHA1.Create() as HashAlgorithm,
				_ => throw new InvalidOperationException(),
			};

			var sb = new StringBuilder(40);
			var hashBytes = hash.ComputeHash(data);
			foreach (var b in hashBytes)
			{
				sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
			}

			return sb.ToString();
		}

		public static string GetHash(this string data, HashType hashType) => GetHash(Encoding.UTF8.GetBytes(data ?? string.Empty), hashType);

		public static void ThrowCollectionEmpty<T>(IEnumerable<T> collection, string paramName)
		{
			if (collection.IsEmpty())
			{
				throw new ArgumentException(CurrentCulture(EveMessages.CollectionInvalid, paramName));
			}
		}

		/*
		public static void ThrowCollectionHasNullItems<T>(IEnumerable<T> collection, string paramName)
			where T : class
		{
			foreach (var item in collection)
			{
				if (item == null)
				{
					throw new ArgumentException(CurrentCulture(CollectionInvalid, paramName));
				}
			}
		}
		*/

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
					throw new ArgumentException(CurrentCulture(EveMessages.CollectionInvalid, paramName));
				}
			}
		}

		public static void ThrowNullOrWhiteSpace(string text, string paramName)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				throw new ArgumentException(CurrentCulture(EveMessages.StringInvalid, paramName));
			}
		}
		#endregion
	}
}
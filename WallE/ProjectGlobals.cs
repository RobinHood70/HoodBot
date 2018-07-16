namespace RobinHood70.WallE
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Security.Cryptography;
	using System.Text;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WallE.Properties.EveMessages;
	using static RobinHood70.WikiCommon.Globals;

	internal static class ProjectGlobals
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "I am!")]
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

		public static void ThrowCollectionEmpty<T>(IEnumerable<T> collection, string paramName)
		{
			if (!collection.HasItems())
			{
				throw new ArgumentException(CurrentCulture(CollectionInvalid, paramName));
			}
		}

		public static void ThrowCollectionHasNullItems<T>(IEnumerable<T> collection, string paramName)
			where T : class
		{
			ThrowCollectionEmpty(collection, paramName);
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
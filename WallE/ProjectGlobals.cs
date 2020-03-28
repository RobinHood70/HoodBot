namespace RobinHood70.WallE
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Properties;
	using static RobinHood70.CommonCode.Globals;

	internal static class ProjectGlobals
	{
		#region Public Methods

		/// <summary>Creates an empty read-only dictionary of the specified type.</summary>
		/// <typeparam name="TKey">The key type.</typeparam>
		/// <typeparam name="TValue">The value type.</typeparam>
		/// <returns>An empty read-only dictionary.</returns>
		public static IReadOnlyDictionary<TKey, TValue> ReadOnlyDictionary<TKey, TValue>()
			where TKey : notnull => new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());

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
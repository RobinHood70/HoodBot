namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.WikiClasses;

	/// <summary>Represents the MediaWiki interwiki map.</summary>
	/// <seealso cref="ReadOnlyKeyedCollection{TKey, TItem}" />
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Same name as MediaWiki.")]
	public class InterwikiMap : ReadOnlyKeyedCollection<string, InterwikiEntry>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="InterwikiMap"/> class.</summary>
		/// <param name="items">The items.</param>
		public InterwikiMap(IEnumerable<InterwikiEntry> items)
			: base(items, StringComparer.Create(CultureInfo.InvariantCulture, true))
		{
		}
		#endregion

		#region Protected Override Methods

		/// <summary>When implemented in a derived class, extracts the key from the specified element.</summary>
		/// <param name="item">The element from which to extract the key.</param>
		/// <returns>The key for the specified element.</returns>
		protected override string GetKeyForItem(InterwikiEntry item) => item?.Prefix;
		#endregion
	}
}
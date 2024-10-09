namespace RobinHood70.Robby
{
	using System;
	using System.Globalization;

	/// <summary>A <see cref="StringComparer"/> class that compares page names in a given <see cref="Namespace">namespace</see>, respecting the rules for that namespace.</summary>
	/// <remarks>Initializes a new instance of the <see cref="PageNameComparer"/> class.</remarks>
	/// <param name="culture">The culture to use for the comparison.</param>
	/// <param name="caseSensitive">Whether the first character of the page name is case-sensitive.</param>
	public class PageNameComparer(CultureInfo culture, bool caseSensitive) : StringComparer
	{
		#region Fields
		private readonly CultureInfo culture = culture;
		private readonly bool caseSensitive = caseSensitive;
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override int Compare(string? x, string? y) => x is null
			? y is null
				? 0
				: -1
			: y is null
				? 1
				: this.InternalCompare(x, y);

		/// <inheritdoc/>
		public override bool Equals(string? x, string? y) => x is null
			? y is null
			: y is not null && x.Length == y.Length && this.InternalCompare(x, y) == 0;

		/// <inheritdoc/>
		public override int GetHashCode(string obj) => Ordinal.GetHashCode(obj);
		#endregion

		#region Private Methods
		private int InternalCompare(string x, string y)
		{
			var compareInfo = this.culture.CompareInfo;
			if (this.caseSensitive)
			{
				return compareInfo.Compare(x, y, CompareOptions.None);
			}

			if (x.Length == 0)
			{
				return y.Length == 0 ? 0 : -1;
			}

			if (y.Length == 0)
			{
				return 1;
			}

			var firstCharCompare = compareInfo.Compare(x, 0, 1, y, 0, 1, CompareOptions.IgnoreCase);
			return firstCharCompare == 0
				? compareInfo.Compare(x, 1, y, 1, CompareOptions.None)
				: firstCharCompare;
		}
		#endregion
	}
}
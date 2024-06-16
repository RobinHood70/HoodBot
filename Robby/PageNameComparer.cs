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
		public override int Compare(string? x, string? y)
		{
			if (x is null)
			{
				return (y is null) ? 0 : -1;
			}

			if (y is null)
			{
				return 1;
			}

			var compareInfo = this.culture.CompareInfo;
			if (this.caseSensitive)
			{
				return compareInfo.Compare(x, y, CompareOptions.None);
			}

			var xFirst = x.Length > 0 ? x[..1] : string.Empty;
			var yFirst = y.Length > 0 ? y[..1] : string.Empty;
			var firstCharCompare = compareInfo.Compare(xFirst, yFirst, CompareOptions.IgnoreCase);
			if (firstCharCompare != 0)
			{
				return firstCharCompare;
			}

			x = x.Length > 1 ? x[1..] : string.Empty;
			y = y.Length > 1 ? y[1..] : string.Empty;
			return compareInfo.Compare(x, y, CompareOptions.None);
		}

		/// <inheritdoc/>
		public override bool Equals(string? x, string? y) => this.Compare(x, y) == 0;

		/// <inheritdoc/>
		public override int GetHashCode(string obj) => Ordinal.GetHashCode(obj);
		#endregion
	}
}
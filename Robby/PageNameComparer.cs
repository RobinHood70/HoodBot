namespace RobinHood70.Robby
{
	using System;
	using System.Globalization;
	using RobinHood70.CommonCode;

	/// <summary>A <see cref="StringComparer"/> class that compares page names in a given <see cref="Namespace">namespace</see>, respecting the rules for that namespace.</summary>
	public class PageNameComparer : StringComparer
	{
		#region FIelds
		private readonly CultureInfo culture;
		private readonly bool caseSensitive;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageNameComparer"/> class.</summary>
		/// <param name="culture">The culture to use for the comparison.</param>
		/// <param name="caseSensitive">Whether the first character of the page name is case-sensitive.</param>
		public PageNameComparer(CultureInfo culture, bool caseSensitive)
		{
			this.culture = culture;
			this.caseSensitive = caseSensitive;
		}
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override int Compare(string? x, string? y) =>
			Globals.NullComparer(x, y) ??
			(this.culture.CompareInfo is var compareInfo &&
			this.caseSensitive
				? compareInfo.Compare(x, y, CompareOptions.None)
				: CompareFull(x!, y!, compareInfo));

		/// <inheritdoc/>
		public override bool Equals(string? x, string? y) => this.Compare(x, y) == 0;

		/// <inheritdoc/>
		public override int GetHashCode(string obj) => Ordinal.GetHashCode(obj);
		#endregion

		#region Private Methods
		private static int CompareFull(string x, string y, CompareInfo compareInfo)
		{
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
		#endregion
	}
}

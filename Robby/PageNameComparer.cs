﻿namespace RobinHood70.Robby
{
	using System;
	using System.Globalization;

	/// <summary>A <see cref="StringComparer"/> class that compares page names in a given <see cref="Namespace">namespace</see>, respecting the rules for that namespace.</summary>
	public class PageNameComparer : StringComparer
	{
		#region FIelds
		private readonly Namespace ns;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageNameComparer"/> class.</summary>
		/// <param name="ns">The namespace the pages belong to.</param>
		public PageNameComparer(Namespace ns) => this.ns = ns;
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override int Compare(string? x, string? y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}

			if (y is null)
			{
				return 1;
			}

			var compareInfo = this.ns.Site.Culture.CompareInfo;
			if (this.ns.CaseSensitive)
			{
				return compareInfo.Compare(x, y, CompareOptions.None);
			}

			var (xFirst, xRemainder) = Split(x);
			var (yFirst, yRemainder) = Split(y);

			var firstCharCompare = compareInfo.Compare(xFirst, yFirst, CompareOptions.IgnoreCase);
			return firstCharCompare != 0
				? firstCharCompare
				: compareInfo.Compare(xRemainder, yRemainder, CompareOptions.None);

			static (string First, string Remainder) Split(string input)
			{
				var first = input.Length > 0 ? input.Substring(0, 1) : string.Empty;
				var remainder = input.Length > 1 ? input[1..] : string.Empty;
				return (first, remainder);
			}
		}

		/// <inheritdoc/>
		public override bool Equals(string? x, string? y) => this.Compare(x, y) == 0;

		/// <inheritdoc/>
		public override int GetHashCode(string obj) => Ordinal.GetHashCode(obj);
		#endregion
	}
}

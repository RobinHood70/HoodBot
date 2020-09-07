namespace RobinHood70.Robby
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
			static int? XYCondition(bool xCondition, bool yCondition)
			{
				return xCondition
					? yCondition
						? 0
						: -1
					: yCondition
						? 1
						: (int?)null;
			}

			if (XYCondition(x is null, y is null) is int retval)
			{
				return retval;
			}

			if (XYCondition(x!.Length == 0, y!.Length == 0) is int retval2)
			{
				return retval2;
			}

			var compareInfo = this.ns.Site.Culture.CompareInfo;
			var firstCharCompare = compareInfo.Compare(x, 0, 1, y, 0, 1, this.ns.CaseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase);
			return firstCharCompare != 0
				? firstCharCompare
				: XYCondition(x!.Length == 1, y!.Length == 1) ?? compareInfo.Compare(x, 1, y, 1, CompareOptions.None);
		}

		/// <inheritdoc/>
		public override bool Equals(string? x, string? y) => this.Compare(x, y) == 0;

		/// <inheritdoc/>
		public override int GetHashCode(string obj) => Ordinal.GetHashCode(obj);
		#endregion
	}
}

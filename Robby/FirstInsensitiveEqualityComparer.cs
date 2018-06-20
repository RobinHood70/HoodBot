namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	/// <summary>An equality comparer that's case-insensitive only for the first character in a string.</summary>
	/// <seealso cref="System.Collections.Generic.EqualityComparer{T}" />
	public class FirstInsensitiveEqualityComparer : EqualityComparer<string>
	{
		private readonly CultureInfo culture;

		/// <summary>Initializes a new instance of the <see cref="FirstInsensitiveEqualityComparer"/> class.</summary>
		/// <param name="culture">The culture.</param>
		public FirstInsensitiveEqualityComparer(CultureInfo culture) => this.culture = culture;

		/// <summary>Determines whether the specified objects are equal.</summary>
		/// <param name="x">The first string to compare.</param>
		/// <param name="y">The second string to compare.</param>
		/// <returns><see langword="true" /> if the specified string are equal, allowing for case-difference in the first character only; otherwise, <see langword="false" />.</returns>
		public override bool Equals(string x, string y)
		{
			if (x.Length > 0 && y.Length > 0)
			{
				if (char.ToUpper(x[0], this.culture) == char.ToUpper(y[0], this.culture))
				{
					if (x.Length > 1 && y.Length > 1)
					{
						return string.Equals(x.Substring(1), y.Substring(1), StringComparison.Ordinal);
					}

					return x.Length == 1 && y.Length == 1;
				}

				return false;
			}

			return x.Length == 0 && y.Length == 0;
		}

		/// <summary>Returns a hash code for this instance.</summary>
		/// <param name="obj">The object.</param>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		public override int GetHashCode(string obj) =>
			obj.Length == 0 ? 0 :
			obj.Length == 1 ? char.ToUpper(obj[0], this.culture).GetHashCode() :
			(char.ToUpper(obj[0], this.culture) + obj.Substring(1)).GetHashCode();
	}
}
namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using static System.Math;

	// Adapted from http://stackoverflow.com/questions/248603/natural-sort-order-in-c-sharp/11624488#11624488
	// For some reason, the default Regex produces a 3-element split with strings like "a100" (a, 100, ""), but this does not affect sorting.
	// A + could also be added to handle explicitly positive numbers, but this can lead to odd sorting like "a100", "a+100", "a100". The fix would be to add "x.Length.CompareTo(y.Length)" if splitX and splitY are equal, but this was unnecessary for my purposes, so left out.

	/// <summary>An IComparer that provides natural sorting for mixed text and numeric strings.</summary>
	public class NaturalSort : IComparer<string>
	{
		#region Fields
		private static readonly Regex NumberRegex = new Regex(@"\d+([\.,]\d+)?");
		#endregion

		#region Public Methods

		/// <summary>Compares two strings and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
		/// <param name="x">The first string to compare.</param>
		/// <param name="y">The second string to compare.</param>
		/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.
		/// Less than zero: <paramref name="x" /> is less than <paramref name="y" />.
		/// Zero: <paramref name="x" /> equals <paramref name="y" />.
		/// Greater than zero: <paramref name="x" /> is greater than <paramref name="y" />.</returns>
		public int Compare(string x, string y)
		{
			// This is not the fastest possible algorithm, since it re-parses strings every time Compare is called, but it has the advantage of being fairly straight-forward.
			if (x == null)
			{
				return y == null ? 0 : -1;
			}

			if (y == null)
			{
				return 1;
			}

			var splitX = NumberRegex.Split(x);
			var splitY = NumberRegex.Split(y);
			var len = Min(splitX.Length, splitY.Length);
			int result;
			for (var i = 0; i < len; i++)
			{
				result = (double.TryParse(splitX[i], out var numX) && double.TryParse(splitY[i], out var numY))
					? numX.CompareTo(numY)
					: string.Compare(splitX[i], splitY[i], StringComparison.CurrentCulture);
				if (result != 0)
				{
					return result;
				}
			}

			return splitX.Length.CompareTo(splitY.Length);
		}
		#endregion
	}
}
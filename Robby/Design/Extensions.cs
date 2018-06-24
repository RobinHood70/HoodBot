namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;

	/// <summary>A generic set of extensions useful in the program's design.</summary>
	public static class Extensions
	{
		/// <summary>Convert a collection of ISimpleTitles to their full page names.</summary>
		/// <param name="titles">The titles to convert.</param>
		/// <returns>An enumeration of the titles converted to their full page names.</returns>
		public static IEnumerable<string> ToFullPageNames(this IEnumerable<ISimpleTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					yield return title.FullPageName;
				}
			}
		}
	}
}
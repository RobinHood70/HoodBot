namespace RobinHood70.Robby
{
	using System.Collections.Generic;
	using Design;
	using WikiCommon;

	/// <summary>Provides extensions across the entire project.</summary>
	public static class Extensions
	{
		#region bool Extensions

		/// <summary>Converts a boolean value to its Tristate equivalent.</summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>A Tristate value representing the equivalent of the boolean parameter.</returns>
		public static Tristate ToTristate(this bool value) => value ? Tristate.True : Tristate.False;
		#endregion

		#region IEnumerable<IWikiTitle> Extensions

		/// <summary>Converts a series of <see cref="IWikiTitle" /> objects to their full page names.</summary>
		/// <param name="titles">The titles to convert.</param>
		/// <returns>The full page names for the titles.</returns>
		public static IEnumerable<string> AsFullPageNames(this IEnumerable<IWikiTitle> titles)
		{
			if (titles != null)
			{
				foreach (var title in titles)
				{
					yield return title.FullPageName;
				}
			}
		}
		#endregion
	}
}

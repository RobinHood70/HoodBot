namespace RobinHood70.Robby
{
	using System.Collections.Generic;
	using Design;
	using WallE;

	/// <summary>Provides extensions across the entire project.</summary>
	public static class Extensions
	{
		#region bool Extensions
		public static Tristate ToTristate(this bool value) => value ? Tristate.True : Tristate.False;
		#endregion

		#region IEnumerable<IWikiTitle> Extensions

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

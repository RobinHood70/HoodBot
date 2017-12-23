namespace RobinHood70.Robby
{
	using System.Collections.Generic;
	using Design;
	using WikiCommon;

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

		#region IEnumerable<Namespace> Extensions
		public static IEnumerable<int> AsIds(this IEnumerable<Namespace> namespaces)
		{
			if (namespaces != null)
			{
				foreach (var ns in namespaces)
				{
					// Most possible calls to this won't want Special and Media namespaces, so assume we should ignore those ones.
					if (ns.Id >= 0)
					{
						yield return ns.Id;
					}
				}
			}
		}
		#endregion
	}
}

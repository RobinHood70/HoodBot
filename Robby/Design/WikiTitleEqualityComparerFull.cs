namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using static WikiCommon.Globals;

	public class WikiTitleEqualityComparerFull : IEqualityComparer<IWikiTitle>
	{
		public bool Equals(IWikiTitle x, IWikiTitle y) =>
			x == null ? x == y :
			y == null ? false :
			x.Namespace.Equals(y.Namespace) && x.PageName.Equals(y.PageName) && x.Key.Equals(y.Key);

		public int GetHashCode(IWikiTitle obj) => obj == null ? 0 : CompositeHashCode(obj.Namespace.GetHashCode(), obj.PageName.GetHashCode(), obj.Key.GetHashCode());
	}
}

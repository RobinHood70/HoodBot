namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using static Globals;

	public class IWikiTitleEqualityComparerFull : IEqualityComparer<IWikiTitle>
	{
		public bool Equals(IWikiTitle x, IWikiTitle y) =>
			x == null ? x == y :
			y == null ? false :
			x.Key == y.Key && x.Namespace == y.Namespace && x.PageName == y.PageName;

		public int GetHashCode(IWikiTitle obj) => obj == null ? 0 : CompositeHashCode(obj.Key.GetHashCode(), obj.Namespace.GetHashCode(), obj.PageName.GetHashCode());
	}
}

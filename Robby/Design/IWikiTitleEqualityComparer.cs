namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using static Globals;

	public class IWikiTitleEqualityComparer : IEqualityComparer<IWikiTitle>
	{
		public bool Equals(IWikiTitle x, IWikiTitle y) =>
			x == null ? x == y :
			y == null ? false :
			x.Namespace == y.Namespace && x.PageName == y.PageName;

		public int GetHashCode(IWikiTitle obj) => obj == null ? 0 : CompositeHashCode(obj.Namespace.GetHashCode(), obj.PageName.GetHashCode());
	}
}

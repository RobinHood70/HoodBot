namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;

	public class IWikiTitleComparerNamespacePageName : IComparer<Title>
	{
		#region Public Methods
		public int Compare(Title x, Title y)
		{
			if (x == null)
			{
				return y == null ? 0 : -1;
			}

			if (y == null)
			{
				return 1;
			}

			if (x.Namespace == null)
			{
				return y.Namespace == null ? 0 : -1;
			}

			if (y.Namespace == null)
			{
				return 1;
			}

			var nsCompare = x.Namespace.Id.CompareTo(y.Namespace.Id);
			if (nsCompare != 0)
			{
				return nsCompare;
			}

			if (x.PageName == null)
			{
				return y.PageName == null ? 0 : -1;
			}

			if (y.PageName == null)
			{
				return 1;
			}

			return string.Compare(x.PageName, y.PageName, StringComparison.Ordinal);
		}
		#endregion
	}
}

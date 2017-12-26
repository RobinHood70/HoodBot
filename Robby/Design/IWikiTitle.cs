namespace RobinHood70.Robby.Design
{
	public interface IWikiTitle
	{
		#region Properties
		string FullPageName { get; }

		string Key { get; }

		Namespace Namespace { get; }

		string PageName { get; }

		Site Site { get; }
		#endregion

		// TODO: Consider whether these should be required methods.
		#region Methods

		// bool IsIdenticalTo(IWikiTitle title);

		// bool IsSameAs(IWikiTitle title);
		#endregion
	}
}
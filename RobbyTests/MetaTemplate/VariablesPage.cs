namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using Design;
	using Pages;

	public class VariablesPage : Page
	{
		#region Constructors
		public VariablesPage(Site site, string fullPageName, PageLoadOptions loadOptions)
			: base(site, fullPageName, loadOptions)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, VariablesResult> VariableSets { get; } = new Dictionary<string, VariablesResult>();
		#endregion
	}
}
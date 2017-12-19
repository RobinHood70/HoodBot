namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using Design;
	using Pages;

	public class VariablesPage : Page
	{
		#region Constructors
		public VariablesPage(Site site, string fullPageName)
			: base(site, fullPageName)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, VariablesResult> VariableSets { get; } = new Dictionary<string, VariablesResult>();
		#endregion
	}
}
namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Same naming convention as WallE.")]
	public class VariablesResult : ReadOnlyDictionary<string, string>
	{
		#region Constructors
		public VariablesResult(IDictionary<string, string> dictionary)
			: base(dictionary)
		{
		}
		#endregion

		#region Public Properties
		public string Subset { get; set; }
		#endregion
	}
}
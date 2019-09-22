namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Same naming convention as WallE.")]
	public class VariablesResult
	{
		#region Constructors
		public VariablesResult(IDictionary<string, string> dictionary, string subset)
		{
			this.Dictionary = new VariableDictionary(dictionary);
			this.Subset = subset;
		}
		#endregion

		#region Public Properties
		public VariableDictionary Dictionary { get; }

		public string Subset { get; }
		#endregion
	}
}
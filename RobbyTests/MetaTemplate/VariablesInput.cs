namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using WallE.Base;

	public class VariablesInput : IPropertyInput, ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<string> Subsets { get; set; }

		public IEnumerable<string> Variables { get; set; }
		#endregion
	}
}
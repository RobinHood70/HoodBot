namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.WallE.Base;

	public class VariablesPageItem : PageItem
	{
		#region Public Properties
		public IReadOnlyList<VariablesResult> Variables { get; set; } = new ReadOnlyCollection<VariablesResult>(new VariablesResult[0]);
		#endregion
	}
}
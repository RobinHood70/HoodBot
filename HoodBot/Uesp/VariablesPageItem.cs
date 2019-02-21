namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.WallE.Base;

	public class VariablesPageItem : PageItem
	{
		#region Public Properties
		public IReadOnlyList<VariablesResult> Variables { get; set; } = new ReadOnlyCollection<VariablesResult>(Array.Empty<VariablesResult>());
		#endregion
	}
}
namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;

	public class VariablesPageItem : PageItem
	{
		#region Constructors
		internal VariablesPageItem(int ns, string title, long pageId)
			: base(ns, title, pageId)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<VariableItem> Variables { get; } = new List<VariableItem>();
		#endregion
	}
}
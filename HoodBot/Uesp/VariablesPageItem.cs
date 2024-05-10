namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;

	public class VariablesPageItem(int ns, string title, long pageId, PageFlags flags) : PageItem(ns, title, pageId, flags)
	{
		#region Fields
		private readonly List<VariableItem> variables = [];
		#endregion

		#region Public Properties
		public IReadOnlyList<VariableItem> Variables => this.variables;
		#endregion

		#region Public Override Methods
		protected override void ParseCustomResult(object output)
		{
			if (output is VariablesResult result)
			{
				this.variables.AddRange(result);
			}
		}
		#endregion
	}
}
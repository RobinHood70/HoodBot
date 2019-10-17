namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
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
		public IReadOnlyList<VariablesResult> Variables { get; internal set; } = new ReadOnlyCollection<VariablesResult>(Array.Empty<VariablesResult>());
		#endregion
	}
}
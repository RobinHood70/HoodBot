﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;

	public interface IPropertyModule : IQueryModule
	{
		#region Methods
		void SetPageOutput(PageItem page);
		#endregion
	}
}

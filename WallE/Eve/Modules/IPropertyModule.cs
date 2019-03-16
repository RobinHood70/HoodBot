#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using RobinHood70.WallE.Base;

	public interface IPropertyModule : IContinuableQueryModule
	{
		#region Methods
		void SetPageOutput(PageItem page);
		#endregion
	}
}

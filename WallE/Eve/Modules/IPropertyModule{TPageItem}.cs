namespace RobinHood70.WallE.Eve.Modules
{
	using Base;

	public interface IPropertyModule<TPageItem> : IPropertyModule
		where TPageItem : PageItem
	{
		#region Methods
		void SetPageOutput(TPageItem page);
		#endregion
	}
}
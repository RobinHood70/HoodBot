#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using RobinHood70.WallE.Base;

	// Property modules will be called repeatedly as each page's data is parsed. Input values will be stable between iterations, but the output being worked on may not. Do not persist output data between calls.
	public abstract class PropModule<TInput> : QueryModule<TInput, PageItem>, IPropertyModule
		where TInput : class, IPropertyInput
	{
		#region Constructors
		protected PropModule(WikiAbstractionLayer wal, TInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string ModuleType { get; } = "prop";
		#endregion

		#region Public Methods
		public void SetPageOutput(PageItem page) => this.Output = page;
		#endregion
	}
}
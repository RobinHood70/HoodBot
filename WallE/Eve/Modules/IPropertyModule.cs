#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;

	public interface IPropertyModule : IContinuableQueryModule
	{
		#region Methods
		void Deserialize(JToken result, PageItem page);
		#endregion
	}
}

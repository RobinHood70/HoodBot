#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;

	// Property modules will be called repeatedly as each page's data is parsed. Input values will be stable between iterations, but the output being worked on may not. Do not persist output data between calls.
	public abstract class PropModule<TInput> : QueryModule<TInput, PageItem>, IPropertyModule
		where TInput : class, IPropertyInput
	{
		#region Constructors
		protected PropModule(WikiAbstractionLayer wal, TInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "prop";
		#endregion

		#region PUblic Virtual Methods
		public virtual void Deserialize(JToken result, PageItem page)
		{
			if (result != null)
			{
				this.DeserializeParentToPage(result, page.NotNull());
				if (result[this.ResultName] is JToken node && node.Type != JTokenType.Null)
				{
					this.DeserializeToPage(node, page);
				}
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void DeserializeToPage(JToken result, PageItem page);
		#endregion

		#region Protected Override Methods
		protected override void DeserializeResult(JToken? result) => throw new InvalidOperationException(EveMessages.CannotDeserializeWithoutPage);
		#endregion

		#region Protected Virtual Methods
		protected virtual void DeserializeParentToPage(JToken parent, PageItem page)
		{
		}
		#endregion
	}
}
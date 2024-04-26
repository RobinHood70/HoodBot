#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;

	// Property modules will be called repeatedly as each page's data is parsed. Input values will be stable between iterations, but the output being worked on may not. Do not persist output data between calls.
	public abstract class PropModule<TInput, TOutput>(WikiAbstractionLayer wal, TInput input, IPageSetGenerator? pageSetGenerator) : QueryModule<TInput, TOutput>(wal, input, pageSetGenerator), IPropertyModule
		where TInput : class, IPropertyInput
		where TOutput : class
	{
		#region Public Properties
		public object? OutputObject => this.Output;
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "prop";
		#endregion

		#region Protected Override Methods
		protected override void DeserializeResult(JToken? result) => throw new InvalidOperationException(EveMessages.CannotDeserializeWithoutPage);
		#endregion
	}
}
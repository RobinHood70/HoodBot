#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;

	// Property modules will be called repeatedly as each page's data is parsed. Input values will be stable between iterations, but the output being worked on may not. Do not persist output data between calls.
	// See ListModuleBase for comments on methods they have in common.
	public abstract class PropListModule<TInput, TOutput, TItem>(WikiAbstractionLayer wal, TInput input, IPageSetGenerator? pageSetGenerator) : PropModule<TInput, TOutput>(wal, input, pageSetGenerator)
		where TInput : class, IPropertyInput
		where TOutput : class, IList<TItem>
		where TItem : class
	{
		#region Protected Abstract Methods
		protected abstract TItem? GetItem(JToken result);

		protected abstract TOutput GetNewList(JToken parent);
		#endregion

		#region Protected Override Methods
		protected override void DeserializeParent(JToken parent) => this.Output = this.GetNewList(parent);

		protected override void DeserializeResult(JToken? result)
		{
			ArgumentNullException.ThrowIfNull(result);
			if (this.Output is null)
			{
				throw new InvalidOperationException(Globals.CurrentCulture(EveMessages.OutputNotInitialized, this.GetType().Name));
			}

			foreach (var value in result)
			{
				if (this.GetItem(value) is TItem item)
				{
					this.Output.Add(item);
				}

				if (this.ItemsRemaining != int.MaxValue && --this.ItemsRemaining < 0)
				{
					break;
				}
			}
		}
		#endregion
	}
}
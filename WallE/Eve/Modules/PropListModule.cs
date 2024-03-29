﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;

	// Property modules will be called repeatedly as each page's data is parsed. Input values will be stable between iterations, but the output being worked on may not. Do not persist output data between calls.
	// See ListModuleBase for comments on methods they have in common.
	public abstract class PropListModule<TInput, TItem> : PropModule<TInput>
		where TInput : class, IPropertyInput
		where TItem : class
	{
		#region Constructors
		protected PropListModule(WikiAbstractionLayer wal, TInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract TItem? GetItem(JToken result, PageItem page);

		protected abstract ICollection<TItem> GetMutableList(PageItem page);
		#endregion

		#region Protected Override Methods
		protected override void DeserializeToPage(JToken result, PageItem page)
		{
			using var enumeration = ((IEnumerable<JToken>)result.NotNull()).GetEnumerator();
			var list = this.GetMutableList(page.NotNull()) ?? throw new InvalidOperationException();
			while (this.ItemsRemaining > 0 && enumeration.MoveNext())
			{
				if (this.GetItem(enumeration.Current, page) is TItem item)
				{
					list.Add(item);
					if (this.ItemsRemaining != int.MaxValue)
					{
						this.ItemsRemaining--;
					}
				}
			}
		}
		#endregion
	}
}
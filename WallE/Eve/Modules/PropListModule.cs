#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using static RobinHood70.WikiCommon.Globals;

	// Property modules will be called repeatedly as each page's data is parsed. Input values will be stable between iterations, but the output being worked on may not. Do not persist output data between calls.
	// See ListModuleBase for comments on methods they have in common.
	public abstract class PropListModule<TInput, TItem> : PropModule<TInput>
		where TInput : class, IPropertyInput
		where TItem : class
	{
		#region Fields
		private readonly List<TItem> myList = new List<TItem>();
		#endregion

		#region Constructors
		protected PropListModule(WikiAbstractionLayer wal, TInput input)
			: this(wal, input, null)
		{
		}

		protected PropListModule(WikiAbstractionLayer wal, TInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Methods
		public override void Deserialize(JToken parent)
		{
			if (this.Output != null)
			{
				this.GetResultsFromCurrentPage();
			}

			base.Deserialize(parent);
		}
		#endregion

		#region Protected Methods

		// Makes a copy of the list or else we just end up handing out the same collection to everything.
		protected IReadOnlyList<TItem> CopyList() => new List<TItem>(this.myList).AsReadOnly();

		protected void ResetItems(IEnumerable<TItem> add)
		{
			ThrowNull(add, nameof(add));
			this.myList.Clear();
			this.myList.AddRange(add);
			this.SetItemsRemaining(this.myList.Count);
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract TItem GetItem(JToken result);

		protected abstract void GetResultsFromCurrentPage();

		protected abstract void SetResultsOnCurrentPage();
		#endregion

		#region Protected Override Methods
		protected override void DeserializeResult(JToken result, PageItem output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));
			using var enumeration = (result as IEnumerable<JToken>).GetEnumerator();
			while (this.ItemsRemaining > 0 && enumeration.MoveNext())
			{
				var item = this.GetItem(enumeration.Current);
				if (item != null)
				{
					this.myList.Add(item);
					if (this.ItemsRemaining != int.MaxValue)
					{
						this.ItemsRemaining--;
					}
				}
			}

			this.SetResultsOnCurrentPage();
		}
		#endregion
	}
}

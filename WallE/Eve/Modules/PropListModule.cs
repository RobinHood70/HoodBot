#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Base;
	using Newtonsoft.Json.Linq;
	using static RobinHood70.Globals;

	// Property modules will be called repeatedly as each page's data is parsed. Input values will be stable between iterations, but the output being worked on may not. Do not persist output data between calls.
	// See ListModuleBase for comments on methods they have in common.
	public abstract class PropListModule<TInput, TItem> : PropModule<TInput>
		where TInput : class
		where TItem : class
	{
		#region Constructors
		protected PropListModule(WikiAbstractionLayer wal, TInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Properties
		protected IList<TItem> MyList { get; } = new List<TItem>();
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
		protected void ResetMyList(IEnumerable<TItem> add)
		{
			ThrowNull(add, nameof(add));
			var list = this.MyList;
			list.Clear();
			foreach (var item in add)
			{
				list.Add(item);
			}

			this.SetItemsRemaining(list.Count);
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

			if (this.ItemsRemaining > 0)
			{
				var list = this.MyList;
				foreach (var node in result)
				{
					var item = this.GetItem(node);
					if (item != null)
					{
						list.Add(item);
						if (this.ItemsRemaining > 0 && this.ItemsRemaining < int.MaxValue)
						{
							this.ItemsRemaining -= this.LoopCount;
						}
					}
				}

				this.SetResultsOnCurrentPage();
			}
		}
		#endregion
	}
}

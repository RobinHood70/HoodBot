#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using static RobinHood70.Globals;

	public abstract class ListModule<TInput, TItem> : QueryModule<TInput, IList<TItem>>
		where TInput : class
		where TItem : class
	{
		#region Constructors
		protected ListModule(WikiAbstractionLayer wal, TInput input)
			: base(wal, input, new List<TItem>())
		{
		}

		protected ListModule(WikiAbstractionLayer wal, TInput input, IList<TItem> output)
			: base(wal, input, output)
		{
		}
		#endregion

		#region Public Override Properties
		protected override string ModuleType { get; } = "list";
		#endregion

		#region Protected Methods
		protected abstract TItem GetItem(JToken result);
		#endregion

		#region Protected Override Methods
		protected override void DeserializeResult(JToken result, IList<TItem> output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));
			if (this.ItemsRemaining > 0)
			{
				foreach (var node in result)
				{
					var item = this.GetItem(node);
					if (item == null)
					{
						this.LoopCount = 0;
					}

					if (this.LoopCount != 0)
					{
						// Null-valued items are allowed as an indication of a faulty entry that should not be added. They will not count against the items remaining.
						output.Add(item);
					}

					// This has to be outside of the check for null so that the last item can be updated properly if it's also the end of the batch.
					// I can't imagine that any wiki query would ever actually exhaust int.MaxValue, but let's not do the equivalent of the Y2K bug.
					if (this.ItemsRemaining > 0 && this.ItemsRemaining != int.MaxValue)
					{
						this.ItemsRemaining -= this.LoopCount;
					}

					if (this.ItemsRemaining == 0)
					{
						break;
					}
				}
			}
		}
		#endregion
	}
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class ListModule<TInput, TItem> : QueryModule<TInput, IList<TItem>>, IContinuableQueryModule
		where TInput : class
		where TItem : class
	{
		#region Constructors
		protected ListModule([ValidatedNotNull] WikiAbstractionLayer wal, [ValidatedNotNull] TInput input)
			: base(wal, input, null)
		{
		}

		protected ListModule([ValidatedNotNull] WikiAbstractionLayer wal, [ValidatedNotNull] TInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string ModuleType { get; } = "list";
		#endregion

		#region Protected Methods
		protected abstract TItem? GetItem(JToken result);
		#endregion

		#region Protected Override Methods
		protected override void DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			this.Output ??= new List<TItem>();
			using var enumerator = result.Children().GetEnumerator();
			while (this.ItemsRemaining > 0 && enumerator.MoveNext())
			{
				// While this could be set up to ehck enumeration.Current and simply not call if it's null, because of the accessibility of GetItem, we have to check the result in GetItem anyway, and it could well return a null value, so it makes more sense to check for null afterwards rather than before.
				var item = this.GetItem(enumerator.Current);
				if (item != null)
				{
					this.Output.Add(item);
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

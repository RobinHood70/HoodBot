#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using static RobinHood70.CommonCode.Globals;

	public abstract class ListModule<TInput, TItem> : QueryModule<TInput, IList<TItem>>, IContinuableQueryModule
		where TInput : class
		where TItem : class
	{
		#region Constructors
		protected ListModule([NotNull, ValidatedNotNull] WikiAbstractionLayer wal, [NotNull, ValidatedNotNull] TInput input)
			: this(wal, input, null)
		{
		}

		protected ListModule([NotNull, ValidatedNotNull] WikiAbstractionLayer wal, [NotNull, ValidatedNotNull] TInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "list";
		#endregion

		#region Protected Methods
		protected abstract TItem? GetItem(JToken result);
		#endregion

		#region Protected Override Methods
		protected override void DeserializeResult(JToken? result)
		{
			ThrowNull(result, nameof(result));
			this.Output ??= new List<TItem>();
			using var enumerator = result.Children().GetEnumerator();
			while (this.ItemsRemaining > 0 && enumerator.MoveNext())
			{
				// While this could be set up to ehck enumeration.Current and simply not call if it's null, because of the accessibility of GetItem, we have to check the result in GetItem anyway, and it could well return a null value, so it makes more sense to check for null afterwards rather than before.
				if (this.GetItem(enumerator.Current) is TItem item)
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
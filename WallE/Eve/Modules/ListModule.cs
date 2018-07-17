#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class ListModule<TInput, TItem> : QueryModule<TInput, IList<TItem>>
		where TInput : class
		where TItem : class
	{
		#region Constructors
		protected ListModule(WikiAbstractionLayer wal, TInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, new List<TItem>(), pageSetGenerator)
		{
		}
		#endregion

		#region Protected Override Properties
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
			using (var enumeration = (result as IEnumerable<JToken>).GetEnumerator())
			{
				while (this.ItemsRemaining > 0 && enumeration.MoveNext())
				{
					var item = this.GetItem(enumeration.Current);
					if (item != null)
					{
						output.Add(item);
						if (this.ItemsRemaining != int.MaxValue)
						{
							this.ItemsRemaining--;
						}
					}
				}
			}
		}
		#endregion
	}
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	public class QueryPageSetInput : PageSetInput, ILimitableInput
	{
		#region Constructors
		public QueryPageSetInput(IEnumerable<string> titles)
			: base(titles)
		{
		}

		public QueryPageSetInput(IGeneratorInput generatorInput)
			: base(generatorInput) => this.CopyGeneratorLimits(generatorInput);

		public QueryPageSetInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: base(generatorInput, titles) => this.CopyGeneratorLimits(generatorInput);

		protected QueryPageSetInput(IEnumerable<long> ids, ListType listType)
			: base(ids, listType)
		{
		}

		protected QueryPageSetInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: base(generatorInput, ids, listType) => this.CopyGeneratorLimits(generatorInput);

		protected QueryPageSetInput(QueryPageSetInput input)
			: base(input)
		{
			ThrowNull(input, nameof(input));
			this.Limit = input.Limit;
			this.MaxItems = input.MaxItems;
		}
		#endregion

		#region Public Properties
		public int Limit { get; set; }

		public int MaxItems { get; set; }
		#endregion

		#region Public Static Methods
		public static QueryPageSetInput FromPageIds(IEnumerable<long> pageIds) => new QueryPageSetInput(pageIds, ListType.PageIds);

		public static QueryPageSetInput FromPageIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new QueryPageSetInput(generator, pageIds, ListType.PageIds);

		public static QueryPageSetInput FromRevisionIds(IEnumerable<long> pageIds) => new QueryPageSetInput(pageIds, ListType.RevisionIds);

		public static QueryPageSetInput FromRevisionIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new QueryPageSetInput(generator, pageIds, ListType.RevisionIds);
		#endregion

		#region Private Methods
		private void CopyGeneratorLimits(IGeneratorInput generatorInput)
		{
			if (generatorInput is ILimitableInput genLimit)
			{
				this.Limit = genLimit.Limit;
				this.MaxItems = genLimit.MaxItems;
			}
		}
		#endregion
	}
}

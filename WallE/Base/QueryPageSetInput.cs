#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class QueryPageSetInput : PageSetInput
	{
		#region Constructors
		public QueryPageSetInput(IEnumerable<string> titles)
			: base(titles)
		{
		}

		public QueryPageSetInput(IGeneratorInput generatorInput)
			: base(generatorInput)
		{
		}

		public QueryPageSetInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: base(generatorInput, titles)
		{
		}

		protected QueryPageSetInput(IEnumerable<long> ids, ListType listType)
			: base(ids, listType)
		{
		}

		protected QueryPageSetInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: base(generatorInput, ids, listType)
		{
		}

		protected QueryPageSetInput(QueryPageSetInput input)
			: base(input)
		{
		}
		#endregion

		#region Public Static Methods
		public static QueryPageSetInput FromPageIds(IEnumerable<long> pageIds) => new(pageIds, ListType.PageIds);

		public static QueryPageSetInput FromPageIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new(generator, pageIds, ListType.PageIds);

		public static QueryPageSetInput FromRevisionIds(IEnumerable<long> revisionIds) => new(revisionIds, ListType.RevisionIds);

		public static QueryPageSetInput FromRevisionIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new(generator, pageIds, ListType.RevisionIds);
		#endregion
	}
}
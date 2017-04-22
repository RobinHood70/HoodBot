#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using static RobinHood70.Globals;

	public class PageSetInput : PageSetInputBase, ILimitableInput
	{
		#region Constructors
		public PageSetInput(IEnumerable<string> titles)
			: base(titles)
		{
		}

		public PageSetInput(IGeneratorInput generatorInput)
			: base(generatorInput)
		{
		}

		public PageSetInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: base(generatorInput, titles)
		{
		}

		protected PageSetInput()
		{
		}

		protected PageSetInput(IEnumerable<long> ids, ListType listType)
			: base(ids, listType)
		{
		}

		protected PageSetInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: base(generatorInput, ids, listType)
		{
		}

		protected PageSetInput(PageSetInput input)
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
		public static PageSetInput FromPageIds(IEnumerable<long> pageIds) => new PageSetInput(pageIds, ListType.PageIds);

		public static PageSetInput FromPageIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new PageSetInput(generator, pageIds, ListType.PageIds);

		public static PageSetInput FromRevisionIds(IEnumerable<long> pageIds) => new PageSetInput(pageIds, ListType.RevisionIds);

		public static PageSetInput FromRevisionIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new PageSetInput(generator, pageIds, ListType.RevisionIds);
		#endregion
	}
}

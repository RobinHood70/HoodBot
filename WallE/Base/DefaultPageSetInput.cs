#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	public class DefaultPageSetInput : PageSetInput, ILimitableInput
	{
		#region Constructors
		public DefaultPageSetInput(IEnumerable<string> titles)
			: base(titles)
		{
		}

		public DefaultPageSetInput(IGeneratorInput generatorInput)
			: base(generatorInput)
		{
		}

		public DefaultPageSetInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: base(generatorInput, titles)
		{
		}

		protected DefaultPageSetInput()
		{
		}

		protected DefaultPageSetInput(IEnumerable<long> ids, ListType listType)
			: base(ids, listType)
		{
		}

		protected DefaultPageSetInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: base(generatorInput, ids, listType)
		{
		}

		protected DefaultPageSetInput(DefaultPageSetInput input)
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
		public static DefaultPageSetInput FromPageIds(IEnumerable<long> pageIds) => new DefaultPageSetInput(pageIds, ListType.PageIds);

		public static DefaultPageSetInput FromPageIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new DefaultPageSetInput(generator, pageIds, ListType.PageIds);

		public static DefaultPageSetInput FromRevisionIds(IEnumerable<long> pageIds) => new DefaultPageSetInput(pageIds, ListType.RevisionIds);

		public static DefaultPageSetInput FromRevisionIds(IEnumerable<long> pageIds, IGeneratorInput generator) => new DefaultPageSetInput(generator, pageIds, ListType.RevisionIds);
		#endregion
	}
}

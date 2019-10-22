#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	// IMPNOTE: Uselang is not implemented. It only ever existed in 1.21-1.24 and served little purpose but to localize a single return message with little info of any value.
	public class WatchInput : PageSetInput
	{
		#region Constructors
		public WatchInput(IEnumerable<string> titles)
			: base(titles)
		{
		}

		public WatchInput(IGeneratorInput generatorInput)
			: base(generatorInput)
		{
		}

		public WatchInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: base(generatorInput, titles)
		{
		}

		protected WatchInput(IEnumerable<long> ids, ListType listType)
			: base(ids, listType)
		{
		}

		protected WatchInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: base(generatorInput, ids, listType)
		{
		}
		#endregion

		#region Public Properties
		public string? Token { get; set; }

		public bool Unwatch { get; set; }
		#endregion

		#region Public Static Methods
		public static WatchInput FromPageIds(IEnumerable<long> ids) => new WatchInput(ids, ListType.PageIds);

		public static WatchInput FromPageIds(IGeneratorInput generatorInput, IEnumerable<long> ids) => new WatchInput(generatorInput, ids, ListType.PageIds);

		public static WatchInput FromRevisionIds(IEnumerable<long> ids) => new WatchInput(ids, ListType.RevisionIds);

		public static WatchInput FromRevisionIds(IGeneratorInput generatorInput, IEnumerable<long> ids) => new WatchInput(generatorInput, ids, ListType.RevisionIds);
		#endregion
	}
}
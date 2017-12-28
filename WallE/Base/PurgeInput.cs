#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	public class PurgeInput : PageSetInputBase
	{
		#region Constructors
		public PurgeInput(IEnumerable<string> titles)
			: base(titles)
		{
		}

		public PurgeInput(IGeneratorInput generatorInput)
			: base(generatorInput)
		{
		}

		public PurgeInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: base(generatorInput, titles)
		{
		}

		protected PurgeInput(IEnumerable<long> ids, ListType listType)
			: base(ids, listType)
		{
		}

		protected PurgeInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: base(generatorInput, ids, listType)
		{
		}
		#endregion

		#region Public Properties
		public PurgeMethod Method { get; set; }
		#endregion

		#region Public Static Methods
		public static PurgeInput FromPageIds(IEnumerable<long> ids) => new PurgeInput(ids, ListType.PageIds);

		public static PurgeInput FromPageIds(IGeneratorInput generatorInput, IEnumerable<long> ids) => new PurgeInput(generatorInput, ids, ListType.PageIds);

		public static PurgeInput FromRevisionIds(IEnumerable<long> ids) => new PurgeInput(ids, ListType.RevisionIds);

		public static PurgeInput FromRevisionIds(IGeneratorInput generatorInput, IEnumerable<long> ids) => new PurgeInput(generatorInput, ids, ListType.RevisionIds);
		#endregion
	}
}
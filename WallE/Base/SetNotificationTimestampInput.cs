#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	// NOTE: This class does not use custom constructors beyond those required for PageSetInputBase. In theory, only one of NewerThanRevisionId, Timestamp, and ToRevisionId should actually be specified at the same time. I can't even imagine what that would look like combined with the existing constructors and static functions.
	public class SetNotificationTimestampInput : PageSetInput
	{
		#region Constructors
		public SetNotificationTimestampInput()
			: base() => this.EntireWatchlist = true;

		public SetNotificationTimestampInput(IEnumerable<string> titles)
			: base(titles)
		{
		}

		public SetNotificationTimestampInput(IGeneratorInput generatorInput)
			: base(generatorInput)
		{
		}

		public SetNotificationTimestampInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: base(generatorInput, titles)
		{
		}

		protected SetNotificationTimestampInput(IEnumerable<long> ids, ListType listType)
			: base(ids, listType)
		{
		}

		protected SetNotificationTimestampInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: base(generatorInput, ids, listType)
		{
		}
		#endregion

		#region Public Properties
		public bool EntireWatchlist { get; set; }

		public long NewerThanRevisionId { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Token { get; set; }

		public long ToRevisionId { get; set; }
		#endregion

		#region Public Static Methods
		public static SetNotificationTimestampInput FromPageIds(IEnumerable<long> ids) => new SetNotificationTimestampInput(ids, ListType.PageIds);

		public static SetNotificationTimestampInput FromPageIds(IGeneratorInput generatorInput, IEnumerable<long> ids) => new SetNotificationTimestampInput(generatorInput, ids, ListType.PageIds);

		public static SetNotificationTimestampInput FromRevisionIds(IEnumerable<long> ids) => new SetNotificationTimestampInput(ids, ListType.RevisionIds);

		public static SetNotificationTimestampInput FromRevisionIds(IGeneratorInput generatorInput, IEnumerable<long> ids) => new SetNotificationTimestampInput(generatorInput, ids, ListType.RevisionIds);
		#endregion
	}
}

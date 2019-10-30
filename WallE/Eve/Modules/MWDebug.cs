namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
    using RobinHood70.WallE.Base;
    using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class MWDebug : ActionModule
	{
		public MWDebug(WikiAbstractionLayer wal)
			: base(wal)
		{
		}

		public override int MinimumVersion => 120;

		public override string Name => "debuginfo";

		protected override RequestType RequestType => throw new InvalidOperationException(); // This should never get called, as this is sort of a fake ActionModule.

		protected override void DeserializeActionExtra(JToken result)
		{
			ThrowNull(result, nameof(result));

			// TODO: Implement this!
			this.Wal.DebugInfo = new DebugInfoResult();
		}
	}
}

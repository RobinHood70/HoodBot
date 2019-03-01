#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionClearHasMsg : ActionModule<NullObject, CustomResult>
	{
		#region Constructors
		public ActionClearHasMsg(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Parameters
		public override int MinimumVersion { get; } = 124;

		public override string Name { get; } = "clearhasmsg";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, NullObject input)
		{
		}

		protected override CustomResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			return new CustomResult((string)result);
		}
		#endregion
	}
}

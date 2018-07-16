#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionRsd : ActionModule<NullObject, CustomResult>
	{
		#region Constructors
		public ActionRsd(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "rsd";
		#endregion

		#region Protected Override Properties
		protected override bool ForceCustomDeserialization { get; } = true;

		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, NullObject input)
		{
			// Custom request which doesn't honour format parameter; remove that one and those that cause warnings.
			ThrowNull(request, nameof(request));
			request.Remove("format");
			request.Remove("formatversion");
			request.Remove("utf8");
		}

		protected override CustomResult DeserializeCustom(string result) => new CustomResult(result);

		protected override CustomResult DeserializeResult(JToken result) => throw new NotSupportedException();
		#endregion
	}
}

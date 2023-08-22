namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class ActionRsd : ActionModule<NullObject, CustomResult>
	{
		#region Constructors
		public ActionRsd(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 117;

		public override string Name => "rsd";
		#endregion

		#region Protected Override Properties
		protected override bool ForceCustomDeserialization => true;

		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, NullObject input)
		{
			// Custom request which doesn't honour format parameter; remove that one and those that cause warnings.
			request
				.NotNull()
				.Remove("format");
			request.Remove("formatversion");
			request.Remove("utf8");
		}

		protected override CustomResult DeserializeCustom(string? result) => new(result);

		protected override CustomResult DeserializeResult(JToken? result) => throw new NotSupportedException();
		#endregion
	}
}
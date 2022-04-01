namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionManageTags : ActionModule<ManageTagsInput, ManageTagsResult>
	{
		#region Constructors
		public ActionManageTags(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 125;

		public override string Name => "managetags";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ManageTagsInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.Add("operation", input.Operation)
				.Add("tag", input.Tag)
				.AddIfNotNull("reason", input.Reason)
				.Add("ignorewarnings", input.IgnoreWarnings)
				.AddHidden("token", input.Token);
		}

		protected override ManageTagsResult DeserializeResult(JToken? result)
		{
			result.ThrowNull();
			return new ManageTagsResult(
				operation: result.MustHaveString("operation"),
				tag: result.MustHaveString("tag"),
				success: result.MustHave("success").GetBCBool(),
				warnings: result["warnings"].GetList<string>(),
				logId: (long?)result["logid"] ?? 0);
		}
		#endregion
	}
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionManageTags : ActionModule<ManageTagsInput, ManageTagsResult>
	{
		#region Constructors
		public ActionManageTags(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "managetags";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ManageTagsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("operation", input.Operation)
				.Add("tag", input.Tag)
				.AddIfNotNull("reason", input.Reason)
				.Add("ignorewarnings", input.IgnoreWarnings)
				.AddHidden("token", input.Token);
		}

		protected override ManageTagsResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new ManageTagsResult()
			{
				Operation = (string)result["operation"],
				Tag = (string)result["tag"],
				Warnings = result["warnings"].AsReadOnlyList<string>(),
				Success = result["success"].AsBCBool(),
				LogId = (long?)result["logid"] ?? 0,
			};
			return output;
		}
		#endregion
	}
}

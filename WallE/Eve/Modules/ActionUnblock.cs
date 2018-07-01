#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// MWVERSION: 1.29
	public class ActionUnblock : ActionModule<UnblockInput, UnblockResult>
	{
		#region Constructors
		public ActionUnblock(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "unblock";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, UnblockInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfPositive("id", input.Id)
				.AddIfPositive("userid", input.UserId)
				.AddIfNotNull("user", input.User)
				.AddIfNotNull("reason", input.Reason)
				.Add("tags", input.Tags)
				.AddHidden("token", input.Token);
		}

		protected override UnblockResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new UnblockResult()
			{
				Id = (long)result["id"],
			};
			if (result["user"].Type == JTokenType.Object)
			{
				output.User = (string)result["user"]["mName"];
				output.UserId = (long?)result["user"]["mId"] ?? -1;
			}
			else
			{
				output.User = (string)result["user"];
				output.UserId = (long)result["userid"];
			}

			output.Reason = (string)result["reason"];

			return output;
		}
		#endregion
	}
}
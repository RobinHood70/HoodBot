#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public class ActionTag : ActionModule<TagInput, IReadOnlyList<TagItem>>
	{
		#region Constructors
		public ActionTag(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "tag";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TagInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("rcid", input.RecentChangesIds)
				.Add("revid", input.RevisionIds)
				.Add("logid", input.LogIds)
				.Add("add", input.Add)
				.Add("remove", input.Remove)
				.AddIfNotNull("reason", input.Reason)
				.AddHidden("token", input.Token);
		}

		protected override IReadOnlyList<TagItem> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var tags = new List<TagItem>();
			if (result.Type == JTokenType.Array)
			{
				foreach (var item in result)
				{
					var tag = new TagItem()
					{
						RevisionId = (long?)item["revid"] ?? 0,
						RecentChangesId = (long?)item["rcid"] ?? 0,
						LogId = (long?)item["logid"] ?? 0,
						Status = (string)item["status"],
						Error = item.GetError(),
						NoOperation = item["noop"].AsBCBool(),
						ActionLogId = (long?)item["actionlogid"] ?? 0,
						Added = item["added"].AsReadOnlyList<string>(),
						Removed = item["removed"].AsReadOnlyList<string>(),
					};
					tags.Add(tag);
				}
			}

			return tags.AsReadOnly();
		}
		#endregion
	}
}
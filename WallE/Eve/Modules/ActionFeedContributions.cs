#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionFeedContributions : ActionModule<FeedContributionsInput, CustomResult>
	{
		#region Constructors
		public ActionFeedContributions(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 127;

		public override string Name { get; } = "feedcontributions";
		#endregion

		#region Protected Override Properties
		protected override bool ForceCustomDeserialization { get; } = true;

		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FeedContributionsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("user", input.User)
				.AddIfNotNull("feedformat", input.FeedFormat)
				.Add("namespace", input.Namespace)
				.AddIfPositive("year", input.Year)
				.AddIfPositive("month", input.Month)
				.Add("tagfilter", input.TagFilter)
				.Add("deletedonly", input.DeletedOnly)
				.Add("toponly", input.TopOnly)
				.Add("newonly", input.NewOnly)
				.AddIf("showsizediff", input.ShowSizeDifference, !this.Wal.Flags.HasFlag(SiteInfoFlags.MiserMode));
		}

		protected override CustomResult DeserializeCustom(string result) => new CustomResult(result);

		protected override CustomResult DeserializeResult(JToken result) => throw new NotSupportedException();
		#endregion
	}
}
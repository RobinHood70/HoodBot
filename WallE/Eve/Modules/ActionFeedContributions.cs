﻿namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class ActionFeedContributions : ActionModule<FeedContributionsInput, CustomResult>
	{
		#region Constructors
		public ActionFeedContributions(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 127;

		public override string Name => "feedcontributions";
		#endregion

		#region Protected Override Properties
		protected override bool ForceCustomDeserialization => true;

		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FeedContributionsInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.AddIfNotNull("user", input.User)
				.AddIfNotNull("feedformat", input.FeedFormat)
				.Add("namespace", input.Namespace)
				.AddIfPositive("year", input.Year)
				.AddIfPositive("month", input.Month)
				.Add("tagfilter", input.TagFilter)
				.Add("deletedonly", input.DeletedOnly)
				.Add("toponly", input.TopOnly)
				.Add("newonly", input.NewOnly)
				.AddIf("showsizediff", input.ShowSizeDifference, !this.Wal.AllSiteInfo?.General?.Flags.HasAnyFlag(SiteInfoFlags.MiserMode) ?? false);
		}

		protected override CustomResult DeserializeCustom(string? result) => new(result);

		protected override CustomResult DeserializeResult(JToken? result) => throw new NotSupportedException();
		#endregion
	}
}
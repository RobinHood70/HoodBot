namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionProtect : ActionModule<ProtectInput, ProtectResult>
	{
		#region Constructors
		public ActionProtect(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "protect";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ProtectInput input)
		{
			var protections = new List<string>();
			var expiry = new List<string>();
			input.ThrowNull(nameof(input));
			if (input.Protections != null)
			{
				foreach (var protection in input.Protections)
				{
					protections.Add(protection.Type + '=' + protection.Level);
					var resolvedExpiry = protection.Expiry.ToMediaWiki() ?? protection.ExpiryRelative ?? "infinite"; // Only use infinite (as empty string) if both are null
					expiry.Add(resolvedExpiry);
				}
			}

			request
				.NotNull(nameof(request))
				.AddIfNotNull("title", input.Title)
				.AddIfPositive("pageid", input.PageId)
				.Add("protections", protections)
				.Add("expiry", expiry)
				.AddIfNotNull("reason", input.Reason)
				.Add("tags", input.Tags)
				.Add("cascade", input.Cascade)
				.Add("watch", input.Watchlist == WatchlistOption.Watch && this.SiteVersion < 117)
				.AddIfPositiveIf("watchlist", input.Watchlist, this.SiteVersion >= 117)
				.AddHidden("token", input.Token);
		}

		protected override ProtectResult DeserializeResult(JToken? result)
		{
			result.ThrowNull(nameof(result));
			var protections = new List<ProtectResultItem>();
			if (result["protections"] is JToken protectionsNode)
			{
				foreach (var protection in protectionsNode)
				{
					if (protection.First is JProperty kvp)
					{
						protections.Add(new ProtectResultItem(
							type: kvp.Name,
							level: (string?)kvp.Value ?? string.Empty,
							expiry: protection["expiry"].GetNullableDate()));
					}
				}
			}

			var title = result.MustHaveString("title");
			return new ProtectResult(
				ns: this.FindRequiredNamespace(title),
				title: title,
				reason: result.MustHaveString("reason"),
				cascade: result["cascade"].GetBCBool(),
				protections: protections);
		}
		#endregion
	}
}
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public class ActionProtect : ActionModule<ProtectInput, ProtectResult>
	{
		#region Constructors
		public ActionProtect(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "protect";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ProtectInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var protections = new List<string>();
			var expiry = new List<string>();
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
				.AddIfNotNull("title", input.Title)
				.AddIfPositive("pageid", input.PageId)
				.Add("protections", protections)
				.AddList("expiry", expiry)
				.AddIfNotNull("reason", input.Reason)
				.Add("tags", input.Tags)
				.Add("cascade", input.Cascade)
				.Add("watch", input.Watchlist == WatchlistOption.Watch && this.SiteVersion < 117)
				.AddIfPositiveIf("watchlist", input.Watchlist, this.SiteVersion >= 117)
				.AddHidden("token", input.Token);
		}

		protected override ProtectResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new ProtectResult()
			{
				Cascade = result["cascade"].AsBCBool(),
				Reason = (string)result["reason"],
				Title = (string)result["title"],
			};
			var protections = result["protections"];
			if (protections != null)
			{
				var list = new List<ProtectResultItem>();
				foreach (var protection in protections)
				{
					var item = new ProtectResultItem();
					var kvp = (JProperty)protection.First;
					item.Type = kvp.Name;
					item.Level = (string)kvp.Value;
					item.Expiry = protection["expiry"].AsDate();

					list.Add(item);
				}

				output.Protections = list.AsReadOnly();
			}

			return output;
		}
		#endregion
	}
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class ActionPurge : ActionModulePageSet<PurgeInput, PurgeResult>
	{
		#region Constructors
		public ActionPurge(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 114;

		public override string Name { get; } = "purge";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestPageSet(Request request, PurgeInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIf("forcelinkupdate", input.Method == PurgeMethod.LinkUpdate, this.SiteVersion >= 118)
				.AddIf("forcerecursivelinkupdate", input.Method == PurgeMethod.RecursiveLinkUpdate, this.SiteVersion >= 122);
		}

		protected override void DeserializePage(JToken result, PurgeResult page)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(page, nameof(page));
			page.Flags =
				result.GetFlag("invalid", PurgeFlags.Invalid) |
				result.GetFlag("missing", PurgeFlags.Missing) |
				result.GetFlag("linkupdate", PurgeFlags.LinkUpdate) |
				result.GetFlag("purged", PurgeFlags.Purged);
			this.Pages.Add(page);
		}
		#endregion
	}
}

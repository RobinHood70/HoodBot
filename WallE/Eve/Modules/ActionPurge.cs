namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionPurge : ActionModulePageSet<PurgeInput, PurgeItem>
	{
		#region Constructors
		public ActionPurge(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 114;

		public override string Name => "purge";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestPageSet(Request request, PurgeInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.AddIf("forcelinkupdate", input.Method == PurgeMethod.LinkUpdate, this.SiteVersion >= 118)
				.AddIf("forcerecursivelinkupdate", input.Method == PurgeMethod.RecursiveLinkUpdate, this.SiteVersion >= 122);
		}

		protected override PurgeItem GetItem(JToken result)
		{
			result.ThrowNull();
			return new PurgeItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				pageId: (long?)result["pageid"] ?? 0,
				flags: result.GetFlags(
					("invalid", PurgeFlags.Invalid),
					("missing", PurgeFlags.Missing),
					("linkupdate", PurgeFlags.LinkUpdate),
					("purged", PurgeFlags.Purged)));
		}
		#endregion
	}
}

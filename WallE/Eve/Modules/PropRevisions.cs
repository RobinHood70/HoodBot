#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropRevisions : PropListModule<RevisionsInput, RevisionItem>, IGeneratorModule
	{
		#region Constructors
		public PropRevisions(WikiAbstractionLayer wal, RevisionsInput input)
			: this(wal, input, null)
		{
		}

		public PropRevisions(WikiAbstractionLayer wal, RevisionsInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator) => this.IsRevisionRange =
				input.Start != null ||
				input.End != null ||
				input.StartId > 0 ||
				input.EndId > 0;
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "revisions";
		#endregion

		#region Internal Properties
		internal bool IsRevisionRange { get; }
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "rv";
		#endregion

		#region Public Static Methods
		public static PropRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is RevisionsInput propInput
				? new PropRevisions(wal, propInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(RevisionsInput), input.GetType().Name);

		public static PropRevisions CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is RevisionsInput propInput
				? new PropRevisions(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(RevisionsInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RevisionsInput input)
		{
			if (this.IsGenerator && this.SiteVersion < 125)
			{
				throw new WikiException(EveMessages.RevisionsGeneratorVersionInvalid);
			}

			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.BuildRevisions(input, this.SiteVersion)
				.AddIfPositive("startid", input.StartId)
				.AddIfPositive("endid", input.EndId)
				.AddIf("token", TokensInput.Rollback, input.GetRollbackToken)
				.AddIfNotNull("tag", input.Tag)
				.AddIf("limit", this.Limit, input.Limit > 0 || input.MaxItems > 1 || this.IsRevisionRange); // TODO: Needs testing when limits/maxitems are actually set to positive values. Limits are weird in this module, but since they're per-query, I believe this should work as written.
		}

		protected override RevisionItem GetItem(JToken result, PageItem page) => result.GetRevision();

		protected override ICollection<RevisionItem> GetMutableList(PageItem page) => (ICollection<RevisionItem>)page.Revisions;
		#endregion
	}
}
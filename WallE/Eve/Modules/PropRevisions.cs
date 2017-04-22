#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static Properties.EveMessages;
	using static RobinHood70.Globals;

	internal class PropRevisions : PropListModule<RevisionsInput, RevisionsItem>, IGeneratorModule
	{
		#region Constructors
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "Validated in base class.")]
		public PropRevisions(WikiAbstractionLayer wal, RevisionsInput input)
			: base(wal, input) => this.IsRevisionRange =
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
		protected override string BasePrefix { get; } = "rv";
		#endregion

		#region Public Static Methods
		public static PropRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropRevisions(wal, input as RevisionsInput);

		public static PropRevisions CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropRevisions(wal, input as RevisionsInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RevisionsInput input)
		{
			if (this.IsGenerator && this.SiteVersion < 125)
			{
				throw new WikiException(RevisionsGeneratorVersionInvalid);
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

		protected override RevisionsItem GetItem(JToken result) => result.GetRevision(this.Output.Title);

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.Revisions);

		protected override void SetResultsOnCurrentPage() => this.Output.Revisions = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}
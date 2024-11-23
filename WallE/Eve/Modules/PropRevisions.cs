namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Design;
using RobinHood70.WallE.Eve;
using RobinHood70.WallE.Properties;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class PropRevisions(WikiAbstractionLayer wal, RevisionsInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<RevisionsInput, RevisionsResult, RevisionItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public PropRevisions(WikiAbstractionLayer wal, RevisionsInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 0;

	public override string Name => "revisions";
	#endregion

	#region Internal Properties
	internal bool IsRevisionRange { get; } =
		input.Start != null ||
		input.End != null ||
		input.StartId > 0 ||
		input.EndId > 0;
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "rv";
	#endregion

	#region Public Static Methods
	public static PropRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (RevisionsInput)input, pageSetGenerator);

	public static PropRevisions CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (RevisionsInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, RevisionsInput input)
	{
		if (this.IsGenerator && this.SiteVersion < 125)
		{
			throw new WikiException(EveMessages.RevisionsGeneratorVersionInvalid);
		}

		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		request
			.BuildRevisions(input, this.SiteVersion)
			.AddIfPositive("startid", input.StartId)
			.AddIfPositive("endid", input.EndId)
			.AddIf("token", TokensInput.Rollback, input.GetRollbackToken)
			.AddIfNotNull("tag", input.Tag)
			.AddIf("limit", this.Limit, (input.Limit > 0 || input.MaxItems > 1 || this.IsRevisionRange) && !this.Limit.OrdinalEquals("0")); // TODO: Needs testing when limits/maxitems are actually set to positive values. Limits are weird in this module, but since they're per-query, I believe this should work as written.
	}

	protected override RevisionItem GetItem(JToken result) => result.GetRevision();

	protected override RevisionsResult GetNewList(JToken parent) => [];
	#endregion
}
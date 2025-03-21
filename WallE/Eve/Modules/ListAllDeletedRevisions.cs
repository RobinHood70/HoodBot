﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Eve;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ListAllDeletedRevisions(WikiAbstractionLayer wal, AllDeletedRevisionsInput input, IPageSetGenerator? pageSetGenerator) : ListModule<AllDeletedRevisionsInput, AllRevisionsItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public ListAllDeletedRevisions(WikiAbstractionLayer wal, AllDeletedRevisionsInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 125;

	public override string Name => "alldeletedrevisions";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "adr";
	#endregion

	#region Public Static Methods
	public static ListAllDeletedRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
		new(wal, (AllDeletedRevisionsInput)input, pageSetGenerator);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, AllDeletedRevisionsInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.BuildRevisions(input, this.SiteVersion)
			.Add("namespace", input.Namespaces)
			.AddIfNotNull("from", input.From)
			.AddIfNotNull("to", input.To)
			.AddIfNotNull("prefix", input.Prefix)
			.AddIfNotNull("tag", input.Tag)
			.Add("generatetitles", input.GenerateTitles);
	}

	protected override AllRevisionsItem? GetItem(JToken result) => result == null
		? null
		: new AllRevisionsItem(
		ns: (int)result.MustHave("ns"),
		title: result.MustHaveString("title"),
		pageId: (long)result.MustHave("pageid"),
		revisions: result.GetRevisions());
	#endregion
}
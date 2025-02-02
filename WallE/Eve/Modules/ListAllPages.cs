﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ListAllPages(WikiAbstractionLayer wal, AllPagesInput input, IPageSetGenerator? pageSetGenerator) : ListModule<AllPagesInput, WikiTitleItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public ListAllPages(WikiAbstractionLayer wal, AllPagesInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 0;

	public override string Name => "allpages";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "ap";
	#endregion

	#region Public Static Methods
	public static ListAllPages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (AllPagesInput)input, pageSetGenerator);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, AllPagesInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfNotNull("from", input.From)
			.AddIfNotNull("to", input.To)
			.AddFilterText("prfiltercascade", "cascading", "noncascading", input.FilterCascading)
			.AddFilterText("filterlanglinks", "withlanglinks", "withoutlanglinks", input.FilterLanguageLinks)
			.AddFilterText("filterredir", "redirects", "nonredirects", input.FilterRedirects)
			.AddIf("maxsize", input.MaximumSize, input.MaximumSize >= 0)
			.AddIf("minsize", input.MinimumSize, input.MinimumSize >= 0)
			.Add("namespace", input.Namespace)
			.AddIfNotNull("prefix", input.Prefix)
			.Add("prlevel", input.ProtectionLevels)
			.Add("prtype", input.ProtectionTypes)
			.AddFilterText("prexpiry", "indefinite", "definite", input.FilterIndefinite)
			.AddIf("dir", "descending", input.SortDescending)
			.Add("limit", this.Limit);
	}

	protected override WikiTitleItem GetItem(JToken result) => result.GetWikiTitle();
	#endregion
}
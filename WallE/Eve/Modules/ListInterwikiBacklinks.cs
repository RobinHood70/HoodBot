﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ListInterwikiBacklinks(WikiAbstractionLayer wal, InterwikiBacklinksInput input, IPageSetGenerator? pageSetGenerator) : ListModule<InterwikiBacklinksInput, InterwikiBacklinksItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public ListInterwikiBacklinks(WikiAbstractionLayer wal, InterwikiBacklinksInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 117;

	public override string Name => "iwbacklinks";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "iwbl";
	#endregion

	#region Public Static Methods
	public static ListInterwikiBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (InterwikiBacklinksInput)input, pageSetGenerator);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, InterwikiBacklinksInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfNotNull("prefix", input.Prefix)
			.AddIfNotNull("title", input.Title)
			.AddFlags("prop", input.Properties)
			.AddIf("dir", "descending", input.SortDescending)
			.Add("limit", this.Limit);
	}

	protected override InterwikiBacklinksItem GetItem(JToken result) => new(
		ns: (int)result.MustHave("ns"),
		title: result.MustHaveString("title"),
		pageId: (long)result.MustHave("pageid"),
		iwPrefix: (string?)result["iwprefix"],
		iwTitle: (string?)result["iwtitle"],
		isRedirect: result["redirect"].GetBCBool());
	#endregion
}
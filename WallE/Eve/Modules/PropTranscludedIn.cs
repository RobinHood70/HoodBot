﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class PropTranscludedIn(WikiAbstractionLayer wal, TranscludedInInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<TranscludedInInput, TranscludedInResult, TranscludedInItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public PropTranscludedIn(WikiAbstractionLayer wal, TranscludedInInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 124;

	public override string Name => "transcludedin";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "ti";
	#endregion

	#region Public Static Methods
	public static PropTranscludedIn CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (TranscludedInInput)input, pageSetGenerator);

	public static PropTranscludedIn CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (TranscludedInInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, TranscludedInInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddFlags("prop", input.Properties)
			.Add("namespace", input.Namespaces)
			.AddFilterPiped("show", "redirect", input.FilterRedirects)
			.Add("limit", this.Limit);
	}

	protected override TranscludedInItem? GetItem(JToken result) => result == null
		? null
		: new TranscludedInItem(
			ns: (int?)result["ns"],
			title: (string?)result["title"],
			pageId: (long?)result["pageid"] ?? 0,
			redirect: result["redirect"].GetBCBool());

	protected override TranscludedInResult GetNewList(JToken parent) => [];
	#endregion
}
﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class PropExternalLinks(WikiAbstractionLayer wal, ExternalLinksInput input) : PropListModule<ExternalLinksInput, ExternalLinksResult, string>(wal, input, null)
{
	#region Public Override Properties
	public override int MinimumVersion => 111;

	public override string Name => "extlinks";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "el";
	#endregion

	#region Public Static Methods
	public static PropExternalLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (ExternalLinksInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, ExternalLinksInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("expandurl", input.ExpandUrl)
			.AddIfNotNull("protocol", input.Protocol)
			.AddIfNotNull("query", input.Query)
			.Add("limit", this.Limit);
	}

	protected override string? GetItem(JToken result) => result.MustHaveBCString("url");

	protected override ExternalLinksResult GetNewList(JToken parent) => [];
	#endregion
}
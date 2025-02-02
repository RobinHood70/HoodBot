﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class PropLanguageLinks(WikiAbstractionLayer wal, LanguageLinksInput input) : PropListModule<LanguageLinksInput, LanguageLinksResult, LanguageLinksItem>(wal, input, null)
{
	#region Public Override Properties
	public override int MinimumVersion => 111;

	public override string Name => "langlinks";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "ll";
	#endregion

	#region Public Static Methods
	public static PropLanguageLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (LanguageLinksInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, LanguageLinksInput input)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		request
			.AddFlagsIf("prop", input.Properties, this.SiteVersion >= 123)
			.AddIf("url", input.Properties.HasAnyFlag(LanguageLinksProperties.Url), this.SiteVersion is >= 117 and < 123)
			.AddIfNotNull("lang", input.Language)
			.AddIfNotNull("title", input.Title)
			.AddIf("dir", "descending", input.SortDescending)
			.AddIfNotNull("inlangcode", input.InLanguageCode)
			.Add("limit", this.Limit);
	}

	protected override LanguageLinksItem? GetItem(JToken result) => result.GetLanguageLink();

	protected override LanguageLinksResult GetNewList(JToken parent) => [];
	#endregion
}
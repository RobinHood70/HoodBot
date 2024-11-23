namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ListPagePropertyNames(WikiAbstractionLayer wal, PagePropertyNamesInput input) : ListModule<PagePropertyNamesInput, string>(wal, input)
{
	#region Public Override Properties
	public override int MinimumVersion => 117;

	public override string Name => "pagepropnames";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "ppn";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, PagePropertyNamesInput input)
	{
		ArgumentNullException.ThrowIfNull(request);
		request.Add("limit", this.Limit);
	}

	protected override string? GetItem(JToken result) => (string?)result?["propname"];
	#endregion
}
namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class PropRedirects(WikiAbstractionLayer wal, RedirectsInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<RedirectsInput, RedirectsResult, RedirectsItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public PropRedirects(WikiAbstractionLayer wal, RedirectsInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 124;

	public override string Name => "redirects";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "rd";
	#endregion

	#region Public Static Methods
	public static PropRedirects CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (RedirectsInput)input, pageSetGenerator);

	public static PropRedirects CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (RedirectsInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, RedirectsInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddFlags("prop", input.Properties)
			.Add("namespace", input.Namespaces)
			.AddFilterPiped("show", "fragment", input.FilterFragments)
			.Add("limit", this.Limit);
	}

	protected override RedirectsItem? GetItem(JToken result) => result == null
		? null
		: new RedirectsItem(
			ns: (int?)result["ns"],
			title: (string?)result["title"],
			pageId: (long?)result["pageid"] ?? 0,
			fragment: (string?)result["fragment"]);

	protected override RedirectsResult GetNewList(JToken parent) => [];
	#endregion
}
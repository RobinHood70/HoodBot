namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ListPrefixSearch(WikiAbstractionLayer wal, PrefixSearchInput input, IPageSetGenerator? pageSetGenerator) : ListModule<PrefixSearchInput, WikiTitleItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public ListPrefixSearch(WikiAbstractionLayer wal, PrefixSearchInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 123;

	public override string Name => "prefixsearch";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "ps";
	#endregion

	#region Public Static Methods
	public static ListPrefixSearch CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (PrefixSearchInput)input, pageSetGenerator);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, PrefixSearchInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfNotNull("search", input.Search)
			.Add("namespace", input.Namespaces)
			.Add("limit", this.Limit);
	}

	protected override WikiTitleItem GetItem(JToken result) => result.GetWikiTitle();
	#endregion
}
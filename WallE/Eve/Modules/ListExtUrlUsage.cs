namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ListExtUrlUsage(WikiAbstractionLayer wal, ExternalUrlUsageInput input, IPageSetGenerator? pageSetGenerator) : ListModule<ExternalUrlUsageInput, ExternalUrlUsageItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public ListExtUrlUsage(WikiAbstractionLayer wal, ExternalUrlUsageInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 111;

	public override string Name => "exturlusage";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "eu";
	#endregion

	#region Public Static Methods
	public static ListExtUrlUsage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (ExternalUrlUsageInput)input, pageSetGenerator);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, ExternalUrlUsageInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddFlags("prop", input.Properties)
			.AddIfNotNull("protocol", input.Protocol)
			.AddIfNotNull("query", input.Query)
			.Add("namespace", input.Namespaces)
			.Add("expandurl", input.ExpandUrl)
			.Add("limit", this.Limit);
	}

	protected override ExternalUrlUsageItem? GetItem(JToken result) => result == null
		? null
		: new ExternalUrlUsageItem((int?)result["ns"], (string?)result["title"], (long?)result["pageid"] ?? 0, (string?)result["url"]);
	#endregion
}
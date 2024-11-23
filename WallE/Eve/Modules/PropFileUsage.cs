namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

// For the time being, the complexity involved in implementing FileUsage, LinksHere, Redirects, and TranscludedIn as derived modules is just not worth it in C#. MediaWiki gets away with it much more easily due to the fact that it's mostly working with strings, along with PHP's looser type system. It may become more worthwhile to implement these modules as inherited in the future, however, or do it like the Revisions classes do and have a static base builder/deserializer but remain otherwise uninherited.
internal sealed class PropFileUsage(WikiAbstractionLayer wal, FileUsageInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<FileUsageInput, FileUsageResult, FileUsageItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public PropFileUsage(WikiAbstractionLayer wal, FileUsageInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 124;

	public override string Name => "fileusage";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "fu";
	#endregion

	#region Public Static Methods
	public static PropFileUsage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (FileUsageInput)input, pageSetGenerator);

	public static PropFileUsage CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (FileUsageInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, FileUsageInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddFlags("prop", input.Properties)
			.Add("namespace", input.Namespaces)
			.AddFilterPiped("show", "redirect", input.FilterRedirects)
			.Add("limit", this.Limit);
	}

	protected override FileUsageItem? GetItem(JToken result) => result == null
		? null
		: new FileUsageItem(
			ns: (int?)result["ns"],
			title: (string?)result["title"],
			pageId: (long?)result["pageid"] ?? 0,
			redirect: result["redirect"].GetBCBool());

	protected override FileUsageResult GetNewList(JToken parent) => [];
	#endregion
}
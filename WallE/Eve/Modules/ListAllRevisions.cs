namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Eve;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ListAllRevisions(WikiAbstractionLayer wal, AllRevisionsInput input, IPageSetGenerator? pageSetGenerator) : ListModule<AllRevisionsInput, AllRevisionsItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public ListAllRevisions(WikiAbstractionLayer wal, AllRevisionsInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 125;

	public override string Name => "allrevisions";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "arv";
	#endregion

	#region Public Static Methods
	public static ListAllRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (AllRevisionsInput)input, pageSetGenerator);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, AllRevisionsInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.BuildRevisions(input, this.SiteVersion)
			.Add("namespace", input.Namespaces)
			.Add("generatetitles", input.GenerateTitles);
	}

	protected override AllRevisionsItem? GetItem(JToken result) => result == null
		? null
		: new AllRevisionsItem(
		ns: (int)result.MustHave("ns"),
		title: result.MustHaveString("title"),
		pageId: (long)result.MustHave("pageid"),
		revisions: result.GetRevisions());
	#endregion
}
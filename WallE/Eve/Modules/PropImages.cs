namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class PropImages(WikiAbstractionLayer wal, ImagesInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<ImagesInput, ImagesResult, IApiTitle>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public PropImages(WikiAbstractionLayer wal, ImagesInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 111;

	public override string Name => "images";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "im";
	#endregion

	#region Public Static Methods
	public static PropImages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (ImagesInput)input, pageSetGenerator);

	public static PropImages CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (ImagesInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, ImagesInput input)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		request
			.AddIf("images", input.Images, this.SiteVersion >= 118)
			.AddIf("dir", "descending", input.SortDescending && this.SiteVersion >= 119)
			.Add("limit", this.Limit);
	}

	protected override IApiTitle GetItem(JToken result) => result.GetWikiTitle();

	protected override ImagesResult GetNewList(JToken parent) => [];
	#endregion
}
namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class PropDuplicateFiles(WikiAbstractionLayer wal, DuplicateFilesInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<DuplicateFilesInput, DuplicateFilesResult, DuplicateFilesItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public PropDuplicateFiles(WikiAbstractionLayer wal, DuplicateFilesInput input)
		: this(wal, input, null)
	{
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 114;

	public override string Name => "duplicatefiles";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "df";
	#endregion

	#region Public Static Methods
	public static PropDuplicateFiles CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (DuplicateFilesInput)input, pageSetGenerator);

	public static PropDuplicateFiles CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (DuplicateFilesInput)input);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, DuplicateFilesInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("localonly", input.LocalOnly)
			.AddIf("dir", "descending", input.SortDescending)
			.Add("limit", this.Limit);
	}

	protected override DuplicateFilesItem? GetItem(JToken result) => result == null
		? null
		: new DuplicateFilesItem(
			name: result.MustHaveString("name"),
			shared: result["shared"].GetBCBool(),
			timestamp: result.MustHaveDate("timestamp"),
			user: result.MustHaveString("user"));

	protected override DuplicateFilesResult GetNewList(JToken parent) => [];
	#endregion
}
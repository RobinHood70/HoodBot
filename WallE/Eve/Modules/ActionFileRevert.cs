namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

// MWVERSION: 1.27
internal sealed class ActionFileRevert(WikiAbstractionLayer wal) : ActionModule<FileRevertInput, FileRevertResult>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 118;

	public override string Name => "filerevert";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, FileRevertInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfNotNull("filename", input.FileName)
			.AddIfNotNull("archivename", input.ArchiveName)
			.AddIfNotNull("comment", input.Comment)
			.AddHidden("token", input.Token);
	}

	protected override FileRevertResult DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return new FileRevertResult(result.MustHaveString("result"));
	}
	#endregion
}
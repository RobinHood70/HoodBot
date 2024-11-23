namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ActionClearHasMsg(WikiAbstractionLayer wal) : ActionModule<NullObject, CustomResult>(wal)
{
	#region Public Override Parameters
	public override int MinimumVersion => 124;

	public override string Name => "clearhasmsg";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, NullObject input)
	{
	}

	protected override CustomResult DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return new CustomResult((string?)result ?? string.Empty);
	}
	#endregion
}
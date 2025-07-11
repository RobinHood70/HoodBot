﻿namespace RobinHood70.WallE.Eve.Modules;

using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ActionLogout(WikiAbstractionLayer wal) : ActionModule<LogoutInput, NullObject>(wal) // Both values are dummy types here, since Logout is the exception to the rule, taking no input and providing no result.
{
	#region Public Override Properties
	public override int MinimumVersion => 0;

	public override string Name => "logout";
	#endregion

	#region Protected Override Properties
	protected override bool GetTimeStamp => false;

	protected override RequestType RequestType => this.SiteVersion is >= 130
		? RequestType.Post
		: RequestType.Get;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, LogoutInput input) => request.AddHiddenIfNotNull("token", input.Token);

	protected override NullObject DeserializeResult(JToken? result) => NullObject.Null;
	#endregion
}
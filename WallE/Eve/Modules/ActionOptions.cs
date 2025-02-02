﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ActionOptions(WikiAbstractionLayer wal) : ActionModule<OptionsInputInternal, NullObject>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 120;

	public override string Name => "options";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, OptionsInputInternal input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("reset", input.Reset)
			.AddIf("resetkinds", input.ResetKinds, this.SiteVersion >= 120)
			.Add("change", input.Change)
			.AddIfNotNull("optionname", input.OptionName)
			.AddIfNotNull("optionvalue", input.OptionValue)
			.AddHidden("token", input.Token);
	}

	protected override NullObject DeserializeResult(JToken? result) => NullObject.Null;
	#endregion
}
namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionImageRotate(WikiAbstractionLayer wal) : ActionModulePageSet<ImageRotateInput, ImageRotateItem>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 121;

	public override string Name => "imagerotate";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestPageSet(Request request, ImageRotateInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("rotation", (input.Rotation % 360 + 360) % 360) // Automatically adjusts negative and out-of-range values
			.AddHidden("token", input.Token);
	}

	protected override ImageRotateItem GetItem(JToken result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return new ImageRotateItem(
			ns: (int)result.MustHave("ns"),
			title: result.MustHaveString("title"),
			pageId: (long?)result["pageid"] ?? 0,
			errorMessage: result["errormessage"].GetWarnings(),
			result: (string?)result["result"],
			flags: result.GetFlags(
				("invalid", ImageRotateFlags.Invalid),
				("missing", ImageRotateFlags.Missing)));
	}
	#endregion
}
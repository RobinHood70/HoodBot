#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionImageRotate : ActionModulePageSet<ImageRotateInput, ImageRotateItem>
	{
		#region Constructors
		public ActionImageRotate(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 121;

		public override string Name => "imagerotate";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestPageSet(Request request, ImageRotateInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("rotation", (input.Rotation % 360 + 360) % 360) // Automatically adjusts negative and out-of-range values
				.AddHidden("token", input.Token);
		}

		protected override ImageRotateItem GetItem(JToken result)
		{
			ThrowNull(result, nameof(result));
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
}

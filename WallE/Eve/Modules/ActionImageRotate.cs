namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionImageRotate : ActionModulePageSet<ImageRotateInput, ImageRotateItem>
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
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.Add("rotation", (input.Rotation % 360 + 360) % 360) // Automatically adjusts negative and out-of-range values
				.AddHidden("token", input.Token);
		}

		protected override ImageRotateItem GetItem(JToken result)
		{
			result.ThrowNull(nameof(result));
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

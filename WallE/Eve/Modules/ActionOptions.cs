#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	public class ActionOptions : ActionModule<OptionsInputInternal, NullObject>
	{
		#region Constructors
		public ActionOptions(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 120;

		public override string Name { get; } = "options";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, OptionsInputInternal input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("reset", input.Reset)
				.AddIf("resetkinds", input.ResetKinds, this.SiteVersion >= 120)
				.Add("change", input.Change)
				.AddIfNotNull("optionname", input.OptionName)
				.AddIfNotNull("optionvalue", input.OptionValue)
				.AddHidden("token", input.Token);
		}

		protected override NullObject DeserializeResult(JToken result) => NullObject.Null;
		#endregion
	}
}

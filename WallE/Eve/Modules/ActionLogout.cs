#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal class ActionLogout : ActionModule<LogoutInput, NullObject> // Both values are dummy types here, since Logout is the exception to the rule, taking no input and providing no result.
	{
		#region Constructors
		public ActionLogout(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "logout";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Properties
		protected override StopCheckMethods StopMethods { get; } = StopCheckMethods.None;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LogoutInput input) => request.AddHiddenIfNotNull("token", input.Token);

		protected override NullObject DeserializeResult(JToken result) => NullObject.Null;
		#endregion
	}
}

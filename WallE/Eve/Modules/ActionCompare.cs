#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionCompare : ActionModuleValued<CompareInput, CompareResult>
	{
		#region Constructors
		public ActionCompare(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 118;

		public override string Name => "compare";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CompareInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfPositive("fromid", input.FromId)
				.AddIfPositive("fromrev", input.FromRevision)
				.AddIfNotNull("fromtitle", input.FromTitle)
				.AddIfPositive("toid", input.ToId)
				.AddIfPositive("torev", input.ToRevision)
				.AddIfNotNull("totitle", input.ToTitle);
		}

		protected override CompareResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			return new CompareResult(
				body: (string?)result["body"],
				fromId: (int?)result["fromid"] ?? 0,
				fromRevision: (int?)result["fromrevid"] ?? 0,
				fromTitle: (string?)result["fromtitle"],
				toId: (int?)result["toid"] ?? 0,
				toRevision: (int?)result["torevid"] ?? 0,
				toTitle: (string?)result["totitle"]);
		}
		#endregion
	}
}

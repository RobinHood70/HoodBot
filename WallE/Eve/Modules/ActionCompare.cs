#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionCompare : ActionModule<CompareInput, CompareResult>
	{
		#region Constructors
		public ActionCompare(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 118;

		public override string Name { get; } = "compare";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
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
			var output = new CompareResult()
			{
				Body = (string)result["body"],
				FromId = (int?)result["fromid"] ?? 0,
				FromRevision = (int?)result["fromrev"] ?? 0,
				FromTitle = (string)result["fromtitle"],
				ToId = (int?)result["toid"] ?? 0,
				ToRevision = (int?)result["torev"] ?? 0,
				ToTitle = (string)result["totitle"],
			};
			return output;
		}
		#endregion
	}
}

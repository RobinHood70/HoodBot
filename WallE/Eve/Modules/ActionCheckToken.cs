namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionCheckToken(WikiAbstractionLayer wal) : ActionModule<CheckTokenInput, CheckTokenResult>(wal)
	{
		#region Public Override Properties
		public override int MinimumVersion => 125;

		public override string Name => "checktoken";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CheckTokenInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
			request
				.AddIfNotNull("type", input.Type)
				.AddIfPositive("maxtokenage", input.MaxTokenAge)
				.AddHidden("token", input.Token);
		}

		protected override CheckTokenResult DeserializeResult(JToken? result)
		{
			ArgumentNullException.ThrowIfNull(result);
			return new CheckTokenResult(
				result: result.MustHaveString("result"),
				generated: result["generated"].GetNullableDate());
		}
		#endregion
	}
}
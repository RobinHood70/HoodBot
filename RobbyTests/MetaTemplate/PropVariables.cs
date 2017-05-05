namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using WallE.Base;
	using WallE.Eve;
	using WallE.Eve.Modules;
	using WallE.RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	public class PropVariables : PropListModule<VariablesInput, VariablesResult>, IGeneratorModule
	{
		#region Constructors
		public PropVariables(WikiAbstractionLayer wal, VariablesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 110;

		public override string Name => "metavars";
		#endregion

		#region Protected Override Properties
		protected override string BasePrefix => "mv";
		#endregion

		#region Public Static Methods
		public static PropVariables CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropVariables(wal, input as VariablesInput);

		public static PropVariables CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropVariables(wal, input as VariablesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, VariablesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("var", input.Variables)
				.Add("subset", input.Subsets)
				.Add("limit", this.Limit);
		}

		protected override VariablesResult GetItem(JToken result)
		{
			ThrowNull(result, nameof(result));
			var vars = result["vars"].ToObject<Dictionary<string, string>>();
			var subset = (string)result["subset"];
			return new VariablesResult(vars) { Subset = subset };
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList((this.Output as VariablesPageItem).Variables);

		protected override void SetResultsOnCurrentPage() => (this.Output as VariablesPageItem).Variables = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}
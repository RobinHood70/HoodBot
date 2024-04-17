namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.Eve.Modules;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	public class PropVariables : PropListModule<VariablesInput, VariableItem>, IGeneratorModule
	{
		#region Constructors
		public PropVariables(WikiAbstractionLayer wal, VariablesInput input)
			: this(wal, input, null)
		{
		}

		public PropVariables(WikiAbstractionLayer wal, VariablesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 110;

		public override string Name => "metavars";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "mv";
		#endregion

		#region Public Static Methods
		public static PropVariables CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (VariablesInput)input, pageSetGenerator);

		public static PropVariables CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (VariablesInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, VariablesInput input)
		{
			input.ThrowNull();
			request.NotNull()
				.Add("var", input.Variables)
				.Add("set", input.Sets)
				.Add("limit", this.Limit);
		}

		protected override VariableItem GetItem(JToken result)
		{
			var vars = result.NotNull()["vars"].GetStringDictionary<string>();
			var set = (string?)result["set"];
			return new VariableItem(vars, set);
		}

		protected override IList<VariableItem> GetMutableList(PageItem page)
		{
			var varPage = page as VariablesPageItem ?? throw new InvalidOperationException();
			return varPage.Variables;
		}
		#endregion
	}
}
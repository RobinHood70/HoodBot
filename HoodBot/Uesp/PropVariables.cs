﻿namespace RobinHood70.HoodBot.Uesp;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Eve;
using RobinHood70.WallE.Eve.Modules;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

public class PropVariables(WikiAbstractionLayer wal, VariablesInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<VariablesInput, VariablesResult, VariableItem>(wal, input, pageSetGenerator), IGeneratorModule
{
	#region Constructors
	public PropVariables(WikiAbstractionLayer wal, VariablesInput input)
		: this(wal, input, null)
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
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("var", input.Variables)
			.Add("set", input.Sets)
			.Add("limit", this.Limit);
	}

	protected override VariableItem GetItem(JToken result)
	{
		ArgumentNullException.ThrowIfNull(result);
		var vars = result["vars"].GetStringDictionary<string>();
		var set = (string?)result["set"];
		return new VariableItem(vars, set);
	}

	protected override VariablesResult GetNewList(JToken parent) => [];
	#endregion
}
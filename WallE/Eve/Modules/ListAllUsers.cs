﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ListAllUsers(WikiAbstractionLayer wal, AllUsersInput input) : ListModule<AllUsersInput, AllUsersItem>(wal, input)
{
	#region Public Override Properties
	public override int MinimumVersion => 111;

	public override string Name => "allusers";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "au";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, AllUsersInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfNotNull("from", input.From)
			.AddIfNotNull("to", input.To)
			.AddIfNotNull("prefix", input.Prefix)
			.AddIf("dir", "descending", input.SortDescending)
			.Add(input.ExcludeGroups ? "excludegroup" : "group", input.Groups)
			.Add("rights", input.Rights)
			.AddFlags("prop", input.Properties)
			.Add("witheditsonly", input.WithEditsOnly)
			.Add("activeusers", input.ActiveUsersOnly)
			.Add("limit", this.Limit);
	}

	protected override AllUsersItem? GetItem(JToken result) => result == null
		? null
		: new AllUsersItem(
			baseUser: result.GetUser(),
			recentActions: (int?)result["recentactions"] ?? (int?)result["recenteditcount"] ?? 0);
	#endregion
}
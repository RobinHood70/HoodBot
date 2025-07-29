namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Properties;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ActionQuery : ActionModule
{
	#region Fields
	private readonly List<IQueryModule> queryModules;
	private readonly MetaUserInfo? userModule;
	private ContinueModule? continueModule;
	#endregion

	#region Constructors
	public ActionQuery(WikiAbstractionLayer wal, IEnumerable<IQueryModule> queryModules)
		: base(wal)
	{
		this.queryModules = [.. queryModules ?? []];
		var props =
			((wal.ValidStopCheckMethods.HasAnyFlag(StopCheckMethods.UserNameCheck) && wal.SiteVersion < 128) ? UserInfoProperties.BlockInfo : UserInfoProperties.None)
			| (wal.ValidStopCheckMethods.HasAnyFlag(StopCheckMethods.TalkCheckQuery) ? UserInfoProperties.HasMsg : UserInfoProperties.None);
		if (props != UserInfoProperties.None)
		{
			UserInfoInput userInfoInput = new() { Properties = props };
			this.userModule = new MetaUserInfo(wal, userInfoInput);
		}

		foreach (var module in this.queryModules)
		{
			if (this.continueModule is null && module is IContinuableQueryModule)
			{
				this.continueModule = wal.ModuleFactory.CreateContinue();
				break;
			}
		}
	}
	#endregion

	#region Public Properties
	public UserInfoResult? UserInfo { get; private set; }
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 0;

	public override string Name => "query";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Get;
	#endregion

	#region Public Static Methods
	public static void CheckActiveModules(WikiAbstractionLayer wal, IEnumerable<IQueryModule> modules)
	{
		using var enumerator = modules.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			// This is a deliberately empty query, so return immediately with no further checking.
			return;
		}

		var hasActiveModule = false;
		do
		{
			var module = enumerator.Current;
			if (module.MinimumVersion == 0 || wal.SiteVersion == 0 || wal.SiteVersion >= module.MinimumVersion)
			{
				hasActiveModule = true;
			}
			else
			{
				wal.AddWarning("query-modulenotsupported", module.GetType().Name);
			}
		}
		while (enumerator.MoveNext());

		if (!hasActiveModule)
		{
			throw new InvalidOperationException(EveMessages.NoSupportedModules);
		}
	}

	public static void CheckResult(JToken parent, IReadOnlyList<IQueryModule> modules)
	{
		if (parent["limits"] is JToken limits)
		{
			var moduleDict = new Dictionary<string, IQueryModule>(modules.Count, StringComparer.Ordinal);
			foreach (var module in modules)
			{
				moduleDict[module.Name] = module;
			}

			foreach (var limit in limits.Children<JProperty>())
			{
				if (moduleDict.TryGetValue(limit.Name, out var module) && module is IContinuableQueryModule continuableModule)
				{
					continuableModule.ModuleLimit = (int)limit.Value;
				}
			}
		}

		// Kludgy workaround for https://phabricator.wikimedia.org/T36356. If there had been more than just this one module, some sort of "Needs deserializing during parent's DeserializeParent" feature could have been added, but that seemed just as kludgy as this for a single faulty module.
		if (parent[ListWatchlistRaw.ModuleName] != null)
		{
			foreach (var module in modules)
			{
				if (module is ListWatchlistRaw watchListModule)
				{
					watchListModule.Deserialize(parent);
				}
			}
		}
	}

	public static bool HandleWarning(string from, string? text, IEnumerable<IQueryModule> queryModules, MetaUserInfo? userModule)
	{
		foreach (var module in queryModules)
		{
			if (module.HandleWarning(from, text))
			{
				return true;
			}
		}

		return userModule?.HandleWarning(from, text) ?? false;
	}
	#endregion

	#region Public Methods
	public void Submit()
	{
		this.Wal.ClearWarnings();
		this.BeforeSubmit();
		do
		{
			var request = this.CreateRequest();
			var response = this.Wal.SendRequest(request);
			if (string.IsNullOrWhiteSpace(response))
			{
				throw new InvalidDataException();
			}

			try
			{
				this.ParseResponse(response);
			}
			catch (JsonReaderException)
			{
				Debug.WriteLine(response);
				throw new InvalidDataException(response.Contains("Just a moment", StringComparison.Ordinal)
					? EveMessages.CloudflareBlock
					: null);
			}
		}
		while (this.continueModule?.Continues ?? false);
	}
	#endregion

	#region Protected Override Methods
	protected override void BeforeSubmit()
	{
		base.BeforeSubmit();
		CheckActiveModules(this.Wal, this.queryModules);
	}

	protected override void DeserializeActionExtra(JToken result)
	{
		ArgumentNullException.ThrowIfNull(result);
		if (result[this.Name] is JToken node && node.Type != JTokenType.Null)
		{
			foreach (var module in this.queryModules)
			{
				module.Deserialize(node);
			}

			if (this.userModule != null)
			{
				this.userModule.Deserialize(node);
				this.UserInfo = this.userModule.Output;
			}
		}

		if (this.continueModule != null)
		{
			this.continueModule = this.continueModule.Deserialize(this.Wal, result);
		}

		CheckResult(result, this.queryModules);
	}

	protected override bool HandleWarning(string from, string text) => HandleWarning(from, text, this.queryModules, this.userModule) || base.HandleWarning(from, text);
	#endregion

	#region Private Methods
	private Request CreateRequest()
	{
		var request = this.CreateBaseRequest();
		foreach (var module in this.queryModules)
		{
			module.BuildRequest(request);
		}

		this.userModule?.BuildRequest(request);
		this.continueModule?.BuildRequest(request);

		return request;
	}

	private void ParseResponse(string response)
	{
		var result = ToJson(response);
		if (result.Type == JTokenType.Object)
		{
			this.DeserializeAction(result);
		}
		else if (!(result is JArray array && array.Count == 0))
		{
			throw new InvalidDataException();
		}
	}
	#endregion
}
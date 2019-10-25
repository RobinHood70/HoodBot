#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionQuery : ActionModule
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
			this.queryModules = new List<IQueryModule>(queryModules ?? Array.Empty<IQueryModule>());
			var props =
				((wal.ValidStopCheckMethods.HasFlag(StopCheckMethods.UserNameCheck) && wal.SiteVersion < 128) ? UserInfoProperties.BlockInfo : UserInfoProperties.None)
				| (wal.ValidStopCheckMethods.HasFlag(StopCheckMethods.TalkCheckQuery) ? UserInfoProperties.HasMsg : UserInfoProperties.None);
			if (props != UserInfoProperties.None)
			{
				var userInfoInput = new UserInfoInput() { Properties = props };
				this.userModule = new MetaUserInfo(wal, userInfoInput);
			}

			foreach (var module in this.queryModules)
			{
				if (module is IContinuableQueryModule)
				{
					this.continueModule = wal.ModuleFactory.CreateContinue();
					break;
				}
			}
		}
		#endregion

		#region Public Properties
		public UserInfoResult? UserInfo { get; protected set; }
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

		public static void CheckParent(JToken parent, IEnumerable<IQueryModule> modules)
		{
			if (parent["limits"] is JToken limits)
			{
				foreach (var limit in limits.Children<JProperty>())
				{
					var value = (int)limit.Value;
					foreach (var module in modules)
					{
						if (module.Name == limit.Name && module is IContinuableQueryModule continuableModule)
						{
							continuableModule.ModuleLimit = value;
							break;
						}
					}
				}
			}

			// Kludgey workaround for https://phabricator.wikimedia.org/T36356. If there had been more than just this one module, some sort of "Needs deserializing during parent's DeserializeParent" feature could have been added, but that seemed just as kludgey as this for a single faulty module.
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

		public static bool HandleWarning(string? from, string? text, IEnumerable<IQueryModule> queryModules, MetaUserInfo? userModule)
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
				this.ParseResponse(response);
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

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			base.DeserializeParent(parent);
			if (this.continueModule != null)
			{
				this.continueModule = this.continueModule.Deserialize(this.Wal, parent);
			}

			CheckParent(parent, this.queryModules);
		}

		protected override bool HandleWarning(string? from, string? text) => HandleWarning(from, text, this.queryModules, this.userModule) ? true : base.HandleWarning(from, text);
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

		private void Deserialize(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			this.DeserializeParent(parent);
			if (parent[this.Name] is JToken result && result.Type != JTokenType.Null)
			{
				foreach (var module in this.queryModules)
				{
					module.Deserialize(result);
				}

				if (this.userModule != null)
				{
					this.userModule.Deserialize(result);
					this.UserInfo = this.userModule.Output;
				}
			}
		}

		private void ParseResponse(string? response)
		{
			var jsonResponse = ToJson(response);
			if (jsonResponse.Type == JTokenType.Object)
			{
				this.Deserialize(jsonResponse);
			}
			else if (!(jsonResponse is JArray array && array.Count == 0))
			{
				throw new InvalidDataException();
			}
		}
		#endregion
	}
}
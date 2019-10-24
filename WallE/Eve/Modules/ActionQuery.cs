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
				this.queryModules.Add(this.userModule);
			}

			foreach (var module in this.queryModules)
			{
				if (module is IContinuableQueryModule)
				{
					this.continueModule = wal.ModuleFactory.CreateContinue();
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

		#region Public Methods
		public void Submit()
		{
			this.Wal.ClearWarnings();
			this.BeforeSubmit();
			var request = this.CreateRequest();
			var response = this.Wal.SendRequest(request);
			this.ParseResponse(response);
			while (this.continueModule?.Continues ?? false)
			{
				request = this.CreateRequest();
				response = this.Wal.SendRequest(request);
				this.ParseResponse(response);
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeSubmit()
		{
			base.BeforeSubmit();
			this.CheckActiveModules();
		}

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			base.DeserializeParent(parent);
			if (this.continueModule != null)
			{
				this.continueModule = this.continueModule.Deserialize(this.Wal, parent);
			}

			if (parent["limits"] is JToken limits)
			{
				foreach (var limit in limits.Children<JProperty>())
				{
					var value = (int)limit.Value;
					foreach (var queryModule in this.queryModules)
					{
						if (queryModule.Name == limit.Name && queryModule is IContinuableQueryModule continuableModule)
						{
							continuableModule.ModuleLimit = value;
							break;
						}
					}
				}
			}

			// Kludgey workaround for https://phabricator.wikimedia.org/T36356. If there had been more than just this one module, some sort of "Needs deserializing during parent's DeserializeParent" feature could have been added, but that seemed just as kludgey as this for a single faulty module.
			if (parent[ListWatchlistRaw.ModuleName] != null && this.queryModules.Find(module => module.Name == ListWatchlistRaw.ModuleName) is ListWatchlistRaw watchListModule)
			{
				watchListModule.Deserialize(parent);
			}
		}

		protected override bool HandleWarning(string from, string text)
		{
			if (text.StartsWith("Action '", StringComparison.Ordinal) && text.EndsWith("' is not allowed for the current user", StringComparison.Ordinal))
			{
				// Swallow all token warnings
				return true;
			}

			foreach (var module in this.queryModules)
			{
				if (module.HandleWarning(from, text))
				{
					return true;
				}
			}

			return base.HandleWarning(from, text);
		}
		#endregion

		#region Private Methods
		private void CheckActiveModules()
		{
			if (this.queryModules.Count > 0)
			{
				// Check if any modules are active. This is done before adding/merging the UserModule, since that would always make it appear that there's an active module.
				var hasActiveModule = false;
				foreach (var module in this.queryModules)
				{
					if (module.MinimumVersion == 0 || this.SiteVersion == 0 || this.SiteVersion >= module.MinimumVersion)
					{
						hasActiveModule = true;
					}
					else
					{
						this.Wal.AddWarning("query-modulenotsupported", module.GetType().Name);
					}
				}

				if (!hasActiveModule)
				{
					throw new InvalidOperationException(EveMessages.NoSupportedModules);
				}
			}
		}

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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropInfo : PropModule<InfoInput>
	{
		#region Fields
		private readonly Dictionary<string, bool> baseActions = new Dictionary<string, bool>();
		#endregion

		#region Constructors
		public PropInfo(WikiAbstractionLayer wal, InfoInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "info";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "in";
		#endregion

		#region Public Static Methods
		public static PropInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropInfo(wal, input as InfoInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, InfoInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			if (input.TestActions != null)
			{
				this.baseActions.Clear();
				foreach (var action in input.TestActions)
				{
					this.baseActions[action] = false;
				}
			}

			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(121, InfoProperties.Watchers)
				.FilterBefore(120, InfoProperties.NotificationTimestamp)
				.Value;
			request
				.AddFlags("prop", prop)
				.AddIf("testactions", input.TestActions, this.SiteVersion >= 125)
				.Add("token", input.Tokens)
				.Add("token", !input.Tokens.HasItems() && this.SiteVersion < 124); // Since enumerable version of add will filter out null values, ensure timestamp is requested if tokens are null/empty.
		}

		protected override void DeserializeParent(JToken parent, PageItem output)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(output, nameof(output));
			if (output.Info != null)
			{
				// We already have an Info from a previous query - do not overwrite it, as the results would be empty and produce invalid information. If needed, this could also be converted to check presense of each response field individually.
				return;
			}

			output.GetWikiTitle(parent);
			var info = new PageInfo()
			{
				ContentModel = (string)parent["contentmodel"],
				Language = (string)parent["pagelanguage"],
				Touched = parent["touched"].AsDate(),
				LastRevisionId = (long?)parent["lastrevid"] ?? 0,
				Flags =
					parent.GetFlag("new", PageInfoFlags.New) |
					parent.GetFlag("readable", PageInfoFlags.Readable) |
					parent.GetFlag("redirect", PageInfoFlags.Redirect) |
					parent.GetFlag("watched", PageInfoFlags.Watched),
				Length = (int?)parent["length"] ?? 0,
				StartTimestamp = parent["starttimestamp"].AsDate(),
				RestrictionTypes = parent["restrictiontypes"].AsReadOnlyList<string>(),
				Watchers = (long?)parent["watchers"] ?? 0,
				NotificationTimestamp = parent["notificationtimestamp"].AsDate(),
				TalkId = (long?)parent["talkid"] ?? 0,
				SubjectId = (long?)parent["subjectid"] ?? 0,
				FullUrl = (Uri)parent["fullurl"],
				EditUrl = (Uri)parent["editurl"],
				CanonicalUrl = (Uri)parent["canonicalurl"],
				Preload = (string)parent["preload"],
				DisplayTitle = (string)parent["displaytitle"],
			};

			var counter = parent["counter"];
			info.Counter = counter?.Type == JTokenType.Integer ? (long?)parent["counter"] ?? -1 : -1;

			var tokens = new Dictionary<string, string>();
			foreach (var token in parent.Children<JProperty>())
			{
				if (token.Name.EndsWith("token", StringComparison.Ordinal))
				{
					tokens.Add(token.Name, (string)token.Value);
				}
			}

			info.Tokens = tokens;

			// Protection can apply even when there's no page
			var protectionNode = parent["protection"];
			var protections = new List<ProtectionsItem>();
			if (protectionNode != null)
			{
				foreach (var result in protectionNode)
				{
					var prot = new ProtectionsItem()
					{
						Type = (string)result["type"],
						Level = (string)result["level"],
						Expiry = result["expiry"].AsDate(),
						Cascading = result["cascade"].AsBCBool(),
						Source = (string)result["source"],
					};
					protections.Add(prot);
				}
			}

			info.Protections = protections;

			// Ensure that all inputs have an output so we get consistent results between JSON1 and JSON2. To cover the corner case where some extension gives unexpected outputs that don't match the input actions, or multiple outputs for a single input, I've initialized the dictionary from the input actions, then updated from there. It is assumed that the programmer will be aware of what they're looking for should these cases ever occur, and will not be bothered by extraneous false values beyond the original input actions.
			var testActions = new Dictionary<string, bool>(this.baseActions);
			var testActionsNode = parent["actions"];
			if (testActionsNode != null)
			{
				foreach (var prop in testActionsNode.Children<JProperty>())
				{
					testActions[prop.Name] = prop.Value.AsBCBool();
				}
			}

			info.TestActions = testActions;
			output.Info = info;
			this.Wal.CurrentTimestamp = info.StartTimestamp;
		}

		protected override void DeserializeResult(JToken result, PageItem output)
		{
		}
		#endregion
	}
}
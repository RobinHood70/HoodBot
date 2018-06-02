#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropInfo : PropModule<InfoInput>
	{
		#region Fields
		private readonly Dictionary<string, bool> baseActions = new Dictionary<string, bool>();
		#endregion

		#region Constructors
		public PropInfo(WikiAbstractionLayer wal, InfoInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "info";
		#endregion

		#region Public Override Properties
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
				.Add("token", input.Tokens.AsReadOnlyCollection().Count == 0 && this.SiteVersion < 124); // Since AddPiped will filter out null values, ensure timestamp is requested if tokens are null but timestamp is requested.
		}

		protected override void DeserializeParent(JToken parent, PageItem output)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(output, nameof(output));
			output.GetWikiTitle(parent);
			var info = new PageInfo()
			{
				ContentModel = (string)parent["contentmodel"],
				Language = (string)parent["pagelanguage"],
				Touched = parent["touched"].AsDate(),
				LastRevisionId = (long?)parent["lastrevid"] ?? 0,
			};
			var counter = parent["counter"];
			info.Counter = counter?.Type == JTokenType.Integer ? (long?)parent["counter"] ?? -1 : -1;
			info.Length = (int?)parent["length"] ?? 0;
			info.Flags =
				parent.GetFlag("new", PageInfoFlags.New) |
				parent.GetFlag("readable", PageInfoFlags.Readable) |
				parent.GetFlag("redirect", PageInfoFlags.Redirect) |
				parent.GetFlag("watched", PageInfoFlags.Watched);
			info.StartTimestamp = parent["starttimestamp"].AsDate();
			this.Wal.CurrentTimestamp = info.StartTimestamp;

			var tokens = new Dictionary<string, string>();
#pragma warning disable IDE0007 // Use implicit type
			foreach (JProperty token in parent)
#pragma warning restore IDE0007 // Use implicit type
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
			info.RestrictionTypes = parent.AsReadOnlyList<string>("restrictiontypes");
			info.Watchers = (long?)parent["watchers"] ?? 0;
			info.NotificationTimestamp = parent["notificationtimestamp"].AsDate();
			info.TalkId = (long?)parent["talkid"] ?? 0;
			info.SubjectId = (long?)parent["subjectid"] ?? 0;
			info.FullUrl = (Uri)parent["fullurl"];
			info.EditUrl = (Uri)parent["editurl"];
			info.CanonicalUrl = (Uri)parent["canonicalurl"];
			info.Preload = (string)parent["preload"];
			info.DisplayTitle = (string)parent["displaytitle"];

			// Ensure that all inputs have an output so we get consistent results between JSON1 and JSON2. To cover the corner case where some extension gives unexpected outputs that don't match the input actions, or multiple outputs for a single input, I've done this as two separate loops. It is assumed that the programmer will be aware of what they're looking for should these cases ever occur, and will not be fooled by extraneous false values under the original input actions.
			var testActions = new Dictionary<string, bool>(this.baseActions);
			var testActionsNode = parent["actions"];
			if (testActionsNode != null)
			{
#pragma warning disable IDE0007 // Use implicit type
				foreach (JProperty prop in testActionsNode)
#pragma warning restore IDE0007 // Use implicit type
				{
					testActions[prop.Name] = prop.Value.AsBCBool();
				}
			}

			info.TestActions = testActions;
			output.Info = info;
		}

		protected override void DeserializeResult(JToken result, PageItem output)
		{
		}
		#endregion
	}
}
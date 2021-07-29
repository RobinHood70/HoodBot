namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropInfo : PropModule<InfoInput>
	{
		#region Fields
		private readonly Dictionary<string, bool> baseActions = new(StringComparer.Ordinal);
		#endregion

		#region Constructors
		public PropInfo(WikiAbstractionLayer wal, InfoInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 0;

		public override string Name => "info";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "in";
		#endregion

		#region Public Static Methods
		public static PropInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (InfoInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, InfoInput input)
		{
			input.ThrowNull(nameof(input));
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
				.NotNull(nameof(request))
				.AddFlags("prop", prop)
				.AddIf("testactions", input.TestActions, this.SiteVersion >= 125)
				.Add("token", input.Tokens)
				.Add("token", input.Tokens.IsEmpty() && this.SiteVersion < 124); // Since enumerable version of add will filter out null values, ensure timestamp is requested if tokens are null/empty.
		}

		protected override void DeserializeParentToPage(JToken parent, PageItem page)
		{
			if (page.NotNull(nameof(page)).Info != null)
			{
				// We already have an Info from a previous query - do not overwrite it, as the results would be empty and produce invalid information. If needed, this could also be converted to check presense of each response field individually.
				return;
			}

			var counter = -1L;
			if (parent.NotNull(nameof(parent))["counter"] is JToken counterNode && counterNode.Type == JTokenType.Integer)
			{
				counter = (long)counterNode;
			}

			var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var token in parent.Children<JProperty>())
			{
				if (token.Name.EndsWith("token", StringComparison.Ordinal))
				{
					tokens.Add(token.Name, (string?)token.Value ?? string.Empty);
				}
			}

			// Protection can apply even when there's no page
			var protections = new List<ProtectionsItem>();
			if (parent["protection"] is JToken protectionNode)
			{
				foreach (var result in protectionNode)
				{
					protections.Add(new ProtectionsItem(
						type: result.MustHaveString("type"),
						level: result.MustHaveString("level"),
						expiry: result.MustHaveDate("expiry"),
						cascading: result["cascade"].GetBCBool(),
						source: (string?)result["source"]));
				}
			}

			// Ensure that all inputs have an output so we get consistent results between JSON1 and JSON2. To cover the corner case where some extension gives unexpected outputs that don't match the input actions, or multiple outputs for a single input, I've initialized the dictionary from the input actions, then updated from there. It is assumed that the programmer will be aware of what they're looking for should these cases ever occur, and will not be bothered by extraneous false values beyond the original input actions.
			var testActions = new Dictionary<string, bool>(this.baseActions, StringComparer.Ordinal);
			if (parent["actions"] is JToken testActionsNode)
			{
				foreach (var prop in testActionsNode.Children<JProperty>())
				{
					testActions[prop.Name] = prop.Value.GetBCBool();
				}
			}

			// If we got a starttimestamp, and it's greater than the current timestamp, update the current timestamp. This is mostly for MW <= 1.23, but could conceivably also happen if base query and info query occur right as the seconds value updates.
			var startTimestamp = parent["starttimestamp"].GetNullableDate();
			if (startTimestamp > this.Wal.CurrentTimestamp)
			{
				this.Wal.CurrentTimestamp = startTimestamp;
			}

			page.Info = new PageInfo(
				canonicalUrl: (Uri?)parent["canonicalurl"],
				contentModel: (string?)parent["contentmodel"],
				counter: counter,
				displayTitle: (string?)parent["displaytitle"],
				editUrl: (Uri?)parent["editurl"],
				flags: parent.GetFlags(
					("new", PageInfoFlags.New),
					("readable", PageInfoFlags.Readable),
					("redirect", PageInfoFlags.Redirect),
					("watched", PageInfoFlags.Watched)),
				fullUrl: (Uri?)parent["fullurl"],
				language: (string?)parent["pagelanguage"],
				lastRevisionId: (long?)parent["lastrevid"] ?? 0,
				length: (int?)parent["length"] ?? 0,
				notificationTimestamp: parent["notificationtimestamp"].GetNullableDate(),
				preload: (string?)parent["preload"],
				protections: protections,
				restrictionTypes: parent["restrictiontypes"].GetList<string>(),
				startTimestamp: startTimestamp,
				subjectId: (long?)parent["subjectid"] ?? 0,
				talkId: (long?)parent["talkid"] ?? 0,
				testActions: testActions,
				tokens: tokens,
				touched: parent["touched"].GetNullableDate(),
				watchers: (long?)parent["watchers"] ?? 0);
		}

		protected override void DeserializeToPage(JToken result, PageItem page)
		{
		}
		#endregion
	}
}
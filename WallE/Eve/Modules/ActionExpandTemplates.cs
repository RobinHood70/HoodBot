#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// HACK: Much of this module remains untested, as there is virtually no documentation on it, and example outputs were mostly impossible to find.
	internal class ActionExpandTemplates : ActionModule<ExpandTemplatesInput, ExpandTemplatesResult>
	{
		#region Constructors
		public ActionExpandTemplates(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "expandtemplates";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ExpandTemplatesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(126, ExpandTemplatesProperties.Modules | ExpandTemplatesProperties.JsConfigVars)
				.FilterBefore(125, ExpandTemplatesProperties.Properties)
				.Value;
			request
				.AddIfNotNull("text", input.Text)
				.AddIfNotNull("title", input.Title)
				.AddIfPositive("revid", input.RevisionId)
				.Add("includecomments", input.IncludeComments)
				.AddFlagsIf("prop", prop, this.SiteVersion >= 124)
				.AddIf("generatexml", input.Properties.HasFlag(ExpandTemplatesProperties.ParseTree), this.SiteVersion < 124);
		}

		protected override ExpandTemplatesResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			return new ExpandTemplatesResult(
				categories: result["categories"].ToReadOnlyList<string>(),
				javaScriptConfigVars: result["jsconfigvars"].ToStringDictionary<string>(),
				moduleScripts: result["modulescripts"].ToReadOnlyList<string>(),
				moduleStyles: result["modulestyles"].ToReadOnlyList<string>(),
				modules: result["modules"].ToReadOnlyList<string>(),
				parseTree: (string?)result["parsetree"],
				properties: result["properties"].ToBCDictionary(),
				timeToLive: TimeSpan.FromSeconds((int?)result["ttl"] ?? 0),
				vol: result["volatile"].ToBCBool(),
				wikiText: (string?)result["wikitext"]);
		}
		#endregion
	}
}

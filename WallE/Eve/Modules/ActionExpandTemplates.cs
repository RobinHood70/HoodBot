namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// HACK: Much of this module remains untested, as there is virtually no documentation on it, and example outputs were mostly impossible to find.
	internal sealed class ActionExpandTemplates(WikiAbstractionLayer wal) : ActionModule<ExpandTemplatesInput, ExpandTemplatesResult>(wal)
	{
		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "expandtemplates";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ExpandTemplatesInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
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
				.AddIf("generatexml", input.Properties.HasAnyFlag(ExpandTemplatesProperties.ParseTree), this.SiteVersion < 124);
		}

		protected override ExpandTemplatesResult DeserializeResult(JToken? result)
		{
			ArgumentNullException.ThrowIfNull(result);
			return new ExpandTemplatesResult(
				categories: result["categories"].GetList<string>(),
				javaScriptConfigVars: result["jsconfigvars"].GetStringDictionary<string>(),
				moduleScripts: result["modulescripts"].GetList<string>(),
				moduleStyles: result["modulestyles"].GetList<string>(),
				modules: result["modules"].GetList<string>(),
				parseTree: (string?)result["parsetree"],
				properties: result["properties"].GetBCDictionary(),
				timeToLive: TimeSpan.FromSeconds((int?)result["ttl"] ?? 0),
				vol: result["volatile"].GetBCBool(),
				wikiText: (string?)result["wikitext"]);
		}
		#endregion
	}
}
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class MetaAllMessages : ListModule<AllMessagesInput, AllMessagesItem>
	{
		#region Fields
		private string? languageCode;
		#endregion

		#region Constructors
		public MetaAllMessages(WikiAbstractionLayer wal, AllMessagesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "allmessages";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "meta";

		protected override string Prefix => "am";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllMessagesInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
			this.languageCode = input.LanguageCode;
			request
				.Add("messages", input.Messages)
				.AddFlags("prop", input.Properties)
				.Add("enableparser", input.EnableParser)
				.Add("nocontent", input.NoContent)
				.Add("includelocal", input.IncludeLocal)
				.Add("args", input.Arguments)
				.AddIfNotNull("filter", input.Filter)
				.AddFilterText("customised", "modified", "unmodified", input.FilterModified)
				.AddIfNotNull("lang", input.LanguageCode)
				.AddIfNotNull("from", input.MessageFrom)
				.AddIfNotNull("to", input.MessageTo)
				.AddIfNotNull("title", input.EnableParserTitle)
				.AddIfNotNull("prefix", input.Prefix);
		}

		protected override AllMessagesItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var name = result.MustHaveString("name");
			var normalizedName = (string?)result["normalizedname"];
			if (normalizedName == null)
			{
				var ci = Globals.GetCulture(this.languageCode ?? this.Wal.AllSiteInfo?.General?.Language);
				normalizedName = name.Replace(' ', '_');
				if (char.IsUpper(normalizedName[0]))
				{
					normalizedName = normalizedName[..1].ToLower(ci) + (name.Length > 1 ? name[1..] : string.Empty);
				}
			}

			return new AllMessagesItem(
				content: result.GetNullableBCString("content"),
				def: (string?)result["default"],
				flags: result.GetFlags(
					("customised", MessageFlags.Customized),
					("defaultmissing", MessageFlags.DefaultMissing),
					("missing", MessageFlags.Missing)),
				name: name,
				normalizedName: normalizedName);
		}
		#endregion
	}
}
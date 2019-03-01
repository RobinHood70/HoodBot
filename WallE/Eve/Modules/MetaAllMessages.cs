#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Globalization;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class MetaAllMessages : ListModule<AllMessagesInput, AllMessagesItem>
	{
		#region Fields
		private string languageCode;
		#endregion

		#region Constructors
		public MetaAllMessages(WikiAbstractionLayer wal, AllMessagesInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "allmessages";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType { get; } = "meta";

		protected override string Prefix { get; } = "am";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllMessagesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
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

		protected override AllMessagesItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new AllMessagesItem()
			{
				Content = (string)result.AsBCContent("content"),
				Default = (string)result["default"],
				Flags =
					result.GetFlag("customised", MessageFlags.Customized) |
					result.GetFlag("defaultmissing", MessageFlags.DefaultMissing) |
					result.GetFlag("missing", MessageFlags.Missing),
				Name = (string)result["name"],
			};

			item.NormalizedName = (string)result["normalizedname"];
			if (item.NormalizedName == null)
			{
				var ci = GetCulture(this.languageCode ?? this.Wal.LanguageCode) ?? CultureInfo.CurrentCulture;
				item.NormalizedName = NormalizeMessageName(item.Name, ci);
			}

			return item;
		}
		#endregion
	}
}

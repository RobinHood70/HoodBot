#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Globalization;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class MetaAllMessages : ListModule<AllMessagesInput, AllMessagesItem>
	{
		#region Constructors
		public MetaAllMessages(WikiAbstractionLayer wal, AllMessagesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "allmessages";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "am";

		protected override string ModuleType { get; } = "meta";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllMessagesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("messages", input.Messages)
				.AddFlags("prop", input.Properties)
				.Add("enableparser", input.EnableParser)
				.Add("nocontent", input.NoContent)
				.Add("includelocal", input.IncludeLocal)
				.Add("args", input.Arguments)
				.AddIfNotNull("filter", input.Filter)
				.AddFilterOptionText("customised", "modified", "unmodified", input.FilterModified)
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

			item.NormalizedName = (string)result["normalizedname"] ?? Normalize(item.Name);
			return item;
		}
		#endregion

		#region Private Methods
		private static string Normalize(string name)
		{
			// Fakes the normalization process using a similar technique.
			if (!string.IsNullOrEmpty(name))
			{
				name = name.Replace(' ', '_');
				if (char.IsUpper(name[0]))
				{
					// We have no knowledge of the wiki's culture, and doing so is complex and would probably be somewhat unreliable, so guess by using current culture.
					name = name.Length == 1 ? char.ToLower(name[0], CultureInfo.CurrentCulture).ToString() : char.ToLower(name[0], CultureInfo.CurrentCulture) + name.Substring(1);
				}
			}

			return name;
		}
		#endregion
	}
}

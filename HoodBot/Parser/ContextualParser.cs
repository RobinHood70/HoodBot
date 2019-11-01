namespace RobinHood70.HoodBot.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using static RobinHood70.WikiCommon.Globals;

	public class ContextualParser : NodeCollection
	{
		#region Constructors
		public ContextualParser()
		{
		}

		public ContextualParser(ISimpleTitle title, NodeCollection nodes)
			: base(null, nodes) => this.Title = title;
		#endregion

		#region Public Properties
		public Dictionary<string, Func<string>> MagicWordResolvers { get; } = new Dictionary<string, Func<string>>();

		public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

		public Dictionary<string, Func<string>> TemplateResolvers { get; } = new Dictionary<string, Func<string>>();

		public ISimpleTitle Title { get; set; }
		#endregion

		#region Public Static Methods
		public static ContextualParser FromPage(Page page) => FromText(page, page?.Text);

		public static ContextualParser FromText(ISimpleTitle title, string text)
		{
			ThrowNull(title, nameof(title));
			ThrowNull(text, nameof(text));
			var nodes = WikiTextParser.Parse(text);
			return new ContextualParser(title, nodes);
		}
		#endregion
	}
}
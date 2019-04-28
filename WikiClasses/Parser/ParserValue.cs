namespace RobinHood70.WikiClasses.Parser
{
	using RobinHood70.WikiClasses.Parser.Nodes;

	public class ParserValue
	{
		public ParserValue(INodeBase node) => this.Node = node;

		public ParserValue(string text) => this.Text = text;

		public INodeBase Node { get; }

		public string Text { get; }
	}
}

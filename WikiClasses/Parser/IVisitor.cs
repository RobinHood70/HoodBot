namespace RobinHood70.WikiClasses.Parser
{
	public interface IVisitor
	{
		void Visit(CommentNode node);

		void Visit(EqualsNode node);

		void Visit(HeaderNode node);

		void Visit(IgnoreNode node);

		void Visit(LinkNode node);

		void Visit(NodeCollection nodes);

		void Visit(ParameterNode node);

		void Visit(TagNode node);

		void Visit(TemplateNode node);

		void Visit(TextNode node);
	}
}

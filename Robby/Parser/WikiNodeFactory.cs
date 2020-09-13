namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>A concrete factory for creating <see cref="IWikiNode"/>s to be added to a <see cref="NodeCollection"/>.</summary>
	/// <seealso cref="IWikiNodeFactory" />
	public class WikiNodeFactory : WikiCommon.Parser.Basic.WikiNodeFactory
	{
		/// <inheritdoc/>
		public override IArgumentNode ArgumentNode(NodeCollection name, IList<IParameterNode> defaultValue) =>
			new SiteArgumentNode(name, defaultValue);

		/// <inheritdoc/>
		public override ILinkNode LinkNode(NodeCollection title, IList<IParameterNode> parameters) =>
			new SiteLinkNode(title, parameters);

		/// <inheritdoc/>
		public override IParameterNode ParameterNode(NodeCollection? name, NodeCollection value) =>
			new SiteParameterNode(name, value);

		/// <inheritdoc/>
		public override ITemplateNode TemplateNode(NodeCollection title, IList<IParameterNode> parameters) =>
			new SiteTemplateNode(title, parameters);
	}
}

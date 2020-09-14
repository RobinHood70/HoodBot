namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>A concrete factory for creating <see cref="IWikiNode"/>s to be added to a <see cref="NodeCollection"/>.</summary>
	/// <seealso cref="IWikiNodeFactory" />
	public class WikiNodeFactory : WikiCommon.Parser.Basic.WikiNodeFactory
	{
		/// <inheritdoc/>
		public override IArgumentNode ArgumentNode(IEnumerable<IWikiNode> name, IList<IParameterNode> defaultValue) =>
			new SiteArgumentNode(this, name, defaultValue);

		/// <inheritdoc/>
		public override ILinkNode LinkNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters) =>
			new SiteLinkNode(this, title, parameters);

		/// <inheritdoc/>
		public override IParameterNode ParameterNode(IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value) =>
			new SiteParameterNode(this, name, value);

		/// <inheritdoc/>
		public override ITemplateNode TemplateNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters) =>
			new SiteTemplateNode(this, title, parameters);
	}
}
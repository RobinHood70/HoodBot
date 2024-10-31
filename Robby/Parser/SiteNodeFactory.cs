namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	/// <summary>A concrete factory for creating <see cref="IWikiNode"/>s to be added to a <see cref="WikiNodeCollection"/>.</summary>
	/// <seealso cref="IWikiNodeFactory" />
	/// <remarks>Initializes a new instance of the <see cref="SiteNodeFactory"/> class.</remarks>
	/// <param name="site">The <see cref="Site"/> value to be passed to those nodes that require it.</param>
	public class SiteNodeFactory(Site site) : WikiNodeFactory
	{
		#region Public Properties

		/// <summary>Gets the site to use for site-specific operations like title resolution.</summary>
		/// <value>The site.</value>
		public Site Site { get; } = site;
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override ILinkNode LinkNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters) =>
			new SiteLinkNode(this, title, parameters);

		/// <inheritdoc/>
		public override ITemplateNode TemplateNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters) =>
			new SiteTemplateNode(this, title, parameters);
		#endregion
	}
}
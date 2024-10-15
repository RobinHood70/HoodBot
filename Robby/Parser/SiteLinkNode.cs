namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	// TODO: This is currently a straight copy of the Basic version. It needs to be reviewd for modifications that might be desired for the contextual version.

	/// <summary>Represents a link, including embedded images.</summary>
	public class SiteLinkNode : LinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteLinkNode"/> class.</summary>
		/// <param name="nodeFactory">The factory to use when creating new nodes (must match the <paramref name="parameters"/> factory).</param>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public SiteLinkNode(SiteNodeFactory nodeFactory, IEnumerable<IWikiNode> title, IList<IParameterNode> parameters)
			: base(nodeFactory, title, parameters)
		{
			this.Title = TitleFactory.FromUnvalidated(nodeFactory.Site, this.GetTitleText());
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the site-specific title value.</summary>
		/// <value>The title value.</value>
		public Title Title { get; }
		#endregion
	}
}
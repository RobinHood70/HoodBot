namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	// TODO: This is currently a straight copy of the Basic version. It needs to be reviewd for modifications that might be desired for the contextual version.

	/// <summary>Represents a link, including embedded images.</summary>
	/// <remarks>Initializes a new instance of the <see cref="SiteLinkNode"/> class.</remarks>
	/// <param name="factory">The factory to use when creating new nodes (must match the <paramref name="parameters"/> factory).</param>
	/// <param name="title">The title.</param>
	/// <param name="parameters">The parameters.</param>
	public class SiteLinkNode(SiteNodeFactory factory, IEnumerable<IWikiNode> title, IList<IParameterNode> parameters) : LinkNode(factory, title, parameters)
	{
		#region Fields
		private Title? title;
		#endregion

		#region Public Properties

		/// <summary>Gets the site-specific title value.</summary>
		/// <value>The title value.</value>
		public Title Title => this.title ??= TitleFactory.FromUnvalidated(factory.Site, this.GetTitleText());
		#endregion
	}
}
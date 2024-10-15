namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	// TODO: Expand class to handle numbered parameters better (or at all, in cases like Remove).
	// TODO: This is currently a straight copy of the Basic version. It needs to be reviewd for modifications that might be desired for the contextual version.

	/// <summary>Represents a template call.</summary>
	public class SiteTemplateNode : TemplateNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteTemplateNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes (must match the <paramref name="parameters"/> factory).</param>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public SiteTemplateNode(SiteNodeFactory factory, IEnumerable<IWikiNode> title, IList<IParameterNode> parameters)
			: base(factory, title, parameters)
		{
			this.Title = TitleFactory.FromUnvalidated(factory.Site[MediaWikiNamespaces.Template], this.GetTitleText());
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the site-specific title value.</summary>
		/// <value>The title value.</value>
		public Title Title { get; }
		#endregion
	}
}
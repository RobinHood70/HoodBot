namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;
	using static RobinHood70.CommonCode.Globals;

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
			ThrowNull(factory, nameof(factory));
			this.TitleValue = Robby.Title.FromName(factory.Site, this.GetTitleText());
		}
		#endregion

		#region Public Properties
		public Title TitleValue { get; }
		#endregion
	}
}
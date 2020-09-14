namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	// TODO: This is currently a straight copy of the Basic version. It needs to be reviewd for modifications that might be desired for the contextual version.

	/// <summary>Represents a link, including embedded images.</summary>
	public class SiteLinkNode : LinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteLinkNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes (must match the <paramref name="parameters"/> factory).</param>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public SiteLinkNode(IWikiNodeFactory factory, IEnumerable<IWikiNode> title, IList<IParameterNode> parameters)
			: base(factory, title, parameters)
		{
		}
		#endregion
	}
}

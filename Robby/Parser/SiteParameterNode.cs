namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	// TODO: This is currently a straight copy of the Basic version. It needs to be reviewd for modifications that might be desired for the contextual version.

	/// <summary>Represents a parameter to a template or link.</summary>
	public class SiteParameterNode : ParameterNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteParameterNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes.</param>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public SiteParameterNode(IWikiNodeFactory factory, IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value)
			: base(factory, name, value)
		{
		}
		#endregion
	}
}
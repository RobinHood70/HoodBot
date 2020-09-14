namespace RobinHood70.Robby.Parser
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	// TODO: This is currently a straight copy of the Basic version. It needs to be reviewd for modifications that might be desired for the contextual version.

	/// <summary>Represents a template argument, such as <c>{{{1|}}}</c>.</summary>
	public class SiteArgumentNode : ArgumentNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteArgumentNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes (must match the <paramref name="defaultValue"/> factory).</param>
		/// <param name="name">The title.</param>
		/// <param name="defaultValue">The default value. May be null or an empty collection. If populated, this should preferentially be either a single ParameterNode or a collection of IWikiNodes representing the default value itself. For compatibility with MediaWiki, it can also be a list of parameter nodes, in which case, these will be added as individual entries to the <see cref="ArgumentNode.ExtraValues"/> collection.</param>
		public SiteArgumentNode(IWikiNodeFactory factory, IEnumerable<IWikiNode> name, IList<IParameterNode> defaultValue)
			: base(factory, name, defaultValue)
		{
		}
		#endregion
	}
}
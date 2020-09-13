namespace RobinHood70.WikiCommon.Parser
{
	/// <summary>Represents a header.</summary>
	public interface IHeaderNode : IWikiNode, IParentNode
	{
		#region Properties

		/// <summary>Gets or sets a value indicating whether this <see cref="IHeaderNode"/> is confirmed (direct text) or possible (template or argument).</summary>
		/// <value><see langword="true"/> if confirmed; otherwise, <see langword="false"/>.</value>
		bool Confirmed { get; set; }

		/// <summary>Gets the level.</summary>
		/// <value>The level. This is equal to the number of visible equals signs.</value>
		int Level { get; }

		/// <summary>Gets the title.</summary>
		/// <value>The title.</value>
		NodeCollection Title { get; }
		#endregion
	}
}
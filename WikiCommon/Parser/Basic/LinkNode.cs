namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Properties;

	/// <summary>Represents a link, including embedded images.</summary>
	public class LinkNode : ILinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes (must match the <paramref name="parameters"/> factory).</param>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public LinkNode(IWikiNodeFactory factory, IEnumerable<IWikiNode> title, IList<IParameterNode> parameters)
		{
			this.Factory = factory.NotNull();
			this.Title = new NodeCollection(factory, title.NotNull());
			this.Parameters = parameters.NotNull();
			foreach (var parameter in parameters)
			{
				if (parameter.Factory != factory)
				{
					throw new InvalidOperationException(Resources.FactoriesMustMatch);
				}
			}
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public IWikiNodeFactory Factory { get; }

		/// <inheritdoc/>
		public IEnumerable<NodeCollection> NodeCollections
		{
			get
			{
				yield return this.Title;
				foreach (var parameter in this.Parameters)
				{
					foreach (var nodeCollection in parameter.NodeCollections)
					{
						yield return nodeCollection;
					}
				}
			}
		}

		/// <inheritdoc/>
		public IList<IParameterNode> Parameters { get; }

		/// <inheritdoc/>
		public NodeCollection Title { get; }
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Parameters.Count == 0 ? "[[Link]]" : $"[[Link|Count = {this.Parameters.Count}]]";
		#endregion
	}
}
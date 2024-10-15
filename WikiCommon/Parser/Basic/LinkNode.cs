namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
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
		public LinkNode(
			[NotNull, ValidatedNotNull] IWikiNodeFactory factory,
			[NotNull, ValidatedNotNull] IEnumerable<IWikiNode> title,
			[NotNull, ValidatedNotNull] IList<IParameterNode> parameters)
		{
			ArgumentNullException.ThrowIfNull(factory);
			ArgumentNullException.ThrowIfNull(title);
			ArgumentNullException.ThrowIfNull(parameters);
			this.Factory = factory;
			this.TitleNodes = new WikiNodeCollection(factory, title);
			this.Parameters = parameters;
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
		public IEnumerable<WikiNodeCollection> NodeCollections
		{
			get
			{
				yield return this.TitleNodes;
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
		public WikiNodeCollection TitleNodes { get; }
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
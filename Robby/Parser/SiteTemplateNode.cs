﻿namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	// TODO: Expand class to handle numbered parameters better (or at all, in cases like Remove).
	// TODO: This is currently a straight copy of the Basic version. It needs to be reviewd for modifications that might be desired for the contextual version.

	/// <summary>Represents a template call.</summary>
	public class SiteTemplateNode : ITemplateNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteTemplateNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public SiteTemplateNode(NodeCollection title, IList<IParameterNode> parameters)
		{
			this.Title = title ?? throw ArgumentNull(nameof(title));
			this.Parameters = parameters ?? throw ArgumentNull(nameof(parameters));
			foreach (var parameter in parameters)
			{
				if (parameter.Factory != title.Factory)
				{
					throw new InvalidOperationException(Resources.FactoriesMustMatch);
				}
			}
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public IWikiNodeFactory Factory => this.Title.Factory;

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
		public override string ToString() => this.Parameters.Count == 0 ? "{{Template}}" : $"{{Template|Count = {this.Parameters.Count}}}";
		#endregion
	}
}
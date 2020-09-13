﻿namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Properties;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Represents a parameter to a template or link.</summary>
	public class ParameterNode : IParameterNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public ParameterNode(NodeCollection? name, NodeCollection value)
		{
			this.Name = name;
			this.Value = value ?? throw ArgumentNull(nameof(value));
			if (name != null && name.Factory != value.Factory)
			{
				throw new InvalidOperationException(Resources.FactoriesMustMatch);
			}
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public bool Anonymous => this.Name == null;

		/// <inheritdoc/>
		public IWikiNodeFactory Factory => this.Value.Factory;

		/// <inheritdoc/>
		public NodeCollection? Name { get; private set; }

		/// <inheritdoc/>
		public IEnumerable<NodeCollection> NodeCollections
		{
			get
			{
				if (this.Name != null)
				{
					yield return this.Name;
				}

				yield return this.Value;
			}
		}

		/// <inheritdoc/>
		public NodeCollection Value { get; }
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <inheritdoc/>
		public void AddName(IEnumerable<IWikiNode> name)
		{
			ThrowNull(name, nameof(name));
			this.Name = this.Factory.NodeCollectionFromNodes(name);
		}

		/// <inheritdoc/>
		public void Anonymize() => this.Name = null;
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			// For simple name=value nodes, display the text; otherwise, display "name" and/or "value" as needed so we're not executing time-consuming processing here.
			var name =
				this.Anonymous
					? string.Empty :
				this.Name?.Count == 1 && this.Name.First is LinkedListNode<IWikiNode> firstName && firstName.Value is TextNode nameNode
					? nameNode.Text
					: "<name>";
			var value = this.Value.Count == 1 && this.Value.First is LinkedListNode<IWikiNode> firstValue && firstValue.Value is TextNode valueNode
				? valueNode.Text
				: "<value>";
			return $"|{name}={value}";
		}
		#endregion
	}
}
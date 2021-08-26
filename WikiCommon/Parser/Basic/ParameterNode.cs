namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>Represents a parameter to a template or link.</summary>
	public class ParameterNode : IParameterNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes.</param>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public ParameterNode(IWikiNodeFactory factory, IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value)
		{
			this.Factory = factory.NotNull(nameof(factory));
			this.Name = name == null ? null : new NodeCollection(factory, name);
			this.Value = new NodeCollection(factory, value.NotNull(nameof(value)));
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public bool Anonymous => this.Name == null;

		/// <inheritdoc/>
		public IWikiNodeFactory Factory { get; }

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
		public void AddName(IEnumerable<IWikiNode> name) => this.Name =
			new NodeCollection(this.Factory, name.NotNull(nameof(name)));

		/// <inheritdoc/>
		public void Anonymize() => this.Name = null;
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			// For simple name=value nodes, display the text; otherwise, display "name" and/or "value" as needed so we're not executing time-consuming processing here.
			var retval =
				this.Value.Count == 1 &&
				this.Value[0] is TextNode valueNode
					? valueNode.Text
					: "<value>";
			if (!this.Anonymous)
			{
				var name =
					this.Name is NodeCollection nameNodes &&
					nameNodes.Count == 1 &&
					nameNodes[0] is TextNode nameNode
						? nameNode.Text
						: "<name>";
				retval = string.Concat(name, "=", retval);
			}

			return retval;
		}
		#endregion
	}
}
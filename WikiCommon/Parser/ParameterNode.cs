namespace RobinHood70.WikiCommon.Parser;

using System;
using System.Collections.Generic;

/// <summary>Represents a parameter to a template or link.</summary>
public class ParameterNode : IWikiNode, IParentNode
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
	/// <param name="factory">The factory to use when creating new nodes.</param>
	/// <param name="name">The name.</param>
	/// <param name="value">The value.</param>
	public ParameterNode(IWikiNodeFactory factory, IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value)
	{
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentNullException.ThrowIfNull(value);
		this.Factory = factory;
		this.Name = name == null ? null : new WikiNodeCollection(factory, name);
		this.Value = new WikiNodeCollection(factory, value);
	}
	#endregion

	#region Public Properties

	/// <summary>Gets a value indicating whether this <see cref="ParameterNode">parameter</see> is anonymous.</summary>
	/// <value><see langword="true"/> if anonymous; otherwise, <see langword="false"/>.</value>
	public bool Anonymous => this.Name == null;

	/// <inheritdoc/>
	public IWikiNodeFactory Factory { get; }

	/// <summary>Gets the name of the parameter, if not anonymous.</summary>
	/// <value>The name.</value>
	public WikiNodeCollection? Name { get; private set; }

	/// <inheritdoc/>
	public IEnumerable<WikiNodeCollection> NodeCollections
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

	/// <summary>Gets the parameter value.</summary>
	/// <value>The value.</value>
	public WikiNodeCollection Value { get; }
	#endregion

	#region Public Methods

	/// <summary>Accepts a visitor to process the node.</summary>
	/// <param name="visitor">The visiting class.</param>
	public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

	/// <summary>Adds a name to a previously anonymous parameter.</summary>
	/// <param name="name">The name.</param>
	public void AddName(IEnumerable<IWikiNode> name)
	{
		ArgumentNullException.ThrowIfNull(name);
		this.Name = new WikiNodeCollection(this.Factory, name);
	}

	/// <summary>Changes the parameter from a named parameter to an anonymous one.</summary>
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
				this.Name is WikiNodeCollection nameNodes &&
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
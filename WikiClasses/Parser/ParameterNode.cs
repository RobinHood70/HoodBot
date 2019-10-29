namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using RobinHood70.WikiClasses.Properties;
	using static WikiCommon.Globals;

	/// <summary>Represents a parameter to a template or link.</summary>
	public class ParameterNode : IWikiNode
	{
		#region Fields
		private int index;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="index">The index.</param>
		/// <param name="value">The value.</param>
		public ParameterNode(int index, IEnumerable<IWikiNode> value)
		{
			this.index = index;
			this.Value = new NodeCollection(this, value ?? throw ArgumentNull(nameof(value)));
		}

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public ParameterNode(IEnumerable<IWikiNode> name, IEnumerable<IWikiNode> value)
		{
			this.Name = new NodeCollection(this, name ?? throw ArgumentNull(nameof(name)));
			this.Value = new NodeCollection(this, value ?? throw ArgumentNull(nameof(value)));
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="ParameterNode">parameter</see> is anonymous.</summary>
		/// <value><c>true</c> if anonymous; otherwise, <c>false</c>.</value>
		public bool Anonymous => this.Name == null;

		/// <summary>Gets or sets the index for anonymous parameters.</summary>
		/// <value>The index.</value>
		/// <remarks>If this value is set to a value greater than zero, <see cref="Name"/> is automatically set to <see langword="null"/>.</remarks>
		public int Index
		{
			get => this.index;
			set
			{
				this.index = value;
				if (value > 0 && this.Name != null)
				{
					this.Name = null;
				}
			}
		}

		/// <summary>Gets the name of the parameter, if not anonymous.</summary>
		/// <value>The name.</value>
		public NodeCollection? Name { get; private set; }

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
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

		/// <summary>Gets the parameter value.</summary>
		/// <value>The value.</value>
		public NodeCollection Value { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new ParameterNode from the provided text.</summary>
		/// <param name="txt">The text of the parameter (without a pipe (<c>|</c>).</param>
		/// <returns>A new ParameterNode.</returns>
		/// <remarks>Due to the way the parser works, this method internally creates a template in order to parse the parameter. If you are calling this method as part of constructing a link or template, it is faster to use their methods and construct the entire object at once.</remarks>
		public static ParameterNode FromText(string txt)
		{
			ThrowNull(txt, nameof(txt));
			var template = TemplateNode.FromParts(string.Empty, new[] { txt });
			return (template.Parameters.Count == 1)
				? template.Parameters[0]
				: throw new InvalidOperationException(CurrentCulture(Resources.MalformedNodeText, nameof(ParameterNode), nameof(FromText)));
		}

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="index">The index.</param>
		/// <param name="value">The value.</param>
		/// <returns>A new ParameterNode.</returns>
		public static ParameterNode FromParts(int index, string value) => new ParameterNode(index, WikiTextParser.Parse(value));

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <returns>A new ParameterNode.</returns>
		public static ParameterNode FromParts(string name, string value) => new ParameterNode(WikiTextParser.Parse(name), WikiTextParser.Parse(value));
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Sets the name from a list of nodes.</summary>
		/// <param name="name">The name. If non-null, <see cref="Index"/> will be set to zero.</param>
		/// <remarks>If the name is currently null, a new NodeCollection will be created; otherwise, the existing collection will be cleared and re-populated, so existing references to Name will remain intact.</remarks>
		public void SetName(string name)
		{
			if (name == null)
			{
				this.Name = null;
			}
			else
			{
				this.SetName(WikiTextParser.Parse(name));
			}
		}

		/// <summary>Sets the name from a list of nodes.</summary>
		/// <param name="name">The name. If non-null, <see cref="Index"/> will be set to zero.</param>
		/// <remarks>If the name is currently null, a new NodeCollection will be created; otherwise, the existing collection will be cleared and re-populated, so existing references to Name will remain intact.</remarks>
		public void SetName(IEnumerable<IWikiNode>? name)
		{
			if (name == null)
			{
				this.Name = null;
			}
			else
			{
				this.index = 0;
				if (this.Name == null)
				{
					this.Name = new NodeCollection(this, name);
				}
				else
				{
					this.Name.Clear();
					this.Name.AddRange(name);
				}
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => "|" + (this.Anonymous ? string.Empty : "name=") + "value";
		#endregion
	}
}

namespace RobinHood70.WikiClasses.Parser
{
	using System.Collections;
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a template argument, such as <c>{{{1|}}}</c>.</summary>
	public class ArgumentNode : IWikiNode, IEnumerable<NodeCollection>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ArgumentNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="defaultValue">The default value.</param>
		public ArgumentNode(NodeCollection title, ParameterNode defaultValue)
			: this(title, new List<ParameterNode>() { defaultValue })
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ArgumentNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="defaultValue">The default value, in the form of a <see cref="ParameterNode"/> collection.</param>
		/// <remarks>This constructor should generally not be used. See remarks at <see cref="AllValues"/>.</remarks>
		public ArgumentNode(NodeCollection title, IList<ParameterNode> defaultValue)
		{
			this.Name = title ?? throw ArgumentNull(nameof(title));
			this.AllValues = defaultValue ?? new List<ParameterNode>();
			title.SetParent(this);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets all values.</summary>
		/// <value>All values.</value>
		/// <remarks>MediaWiki uses the same object for templates and arguments. This parser does not, but it retains the same concept in order to ensure compatibility in the event that we're parsing a malformed argument like <c>{{{1|a|b}}}</c>. Do not use this constructor unless that's the case.</remarks>
		public IList<ParameterNode> AllValues { get; }

		/// <summary>Gets the default value.</summary>
		/// <value>The default value.</value>
		/// <remarks>MediaWiki will parse a default value like <c>{{{1|a=b}}}</c> as a parameter with name <c>a</c> and value <c>b</c>, so we're echoing that behaviour.</remarks>
		public NodeCollection DefaultValue => this.AllValues.Count == 0 ? null : this.AllValues[0].Value;

		/// <summary>Gets the name of the argument.</summary>
		/// <value>The argument name.</value>
		public NodeCollection Name { get; }
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<NodeCollection> GetEnumerator()
		{
			if (this.Name != null)
			{
				yield return this.Name;
			}

			foreach (var value in this.AllValues)
			{
				foreach (var valueNode in value)
				{
					yield return valueNode;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			var retval = "{{{arg";
			var def = this.DefaultValue;
			if (def != null)
			{
				retval += '|';
				if (def.Count > 0)
				{
					retval += "default";
				}
			}

			return retval + "}}}";
		}
		#endregion
	}
}

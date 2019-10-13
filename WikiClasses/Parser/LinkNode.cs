namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a link, including embedded images.</summary>
	public class LinkNode : IWikiNode, IBacklinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public LinkNode(IEnumerable<IWikiNode> title, IList<ParameterNode> parameters)
		{
			this.Title = new NodeCollection(this, title ?? throw ArgumentNull(nameof(title)));
			this.Parameters = parameters ?? new List<ParameterNode>();
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the parameters.</summary>
		/// <value>The parameters.</value>
		public IList<ParameterNode> Parameters { get; }

		/// <summary>Gets the title.</summary>
		/// <value>The title.</value>
		public NodeCollection Title { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new LinkNode from the provided text.</summary>
		/// <param name="txt">The text of the link, including surrounding brackets (<c>[[...]]</c>).</param>
		/// <returns>A new LinkNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c>, or any variant thereof).</exception>
		public static LinkNode FromText(string txt) => WikiTextParser.SingleNode<LinkNode>(txt);

		/// <summary>Creates a new LinkNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static LinkNode FromParts(string title) => FromParts(title);

		/// <summary>Creates a new LinkNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="displayText">The display text for the link.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static LinkNode FromParts(string title, string displayText) => FromParts(title, new[] { displayText });

		/// <summary>Creates a new LinkNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The default value.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static LinkNode FromParts(string title, IEnumerable<string> parameters)
		{
			ThrowNull(title, nameof(title));
			ThrowNull(parameters, nameof(parameters));
			var linkText = "[[" + title;
			if (parameters != null)
			{
				var paramText = string.Join("|", parameters);
				if (paramText.Length > 0)
				{
					linkText += "|" + paramText;
				}
			}

			linkText += "]]";

			return FromText(linkText);
		}
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<NodeCollection> GetEnumerator()
		{
			if (this.Title != null)
			{
				yield return this.Title;
			}

			foreach (var param in this.Parameters)
			{
				foreach (var paramNode in param)
				{
					yield return paramNode;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Parameters.Count == 0 ? "[[Link]]" : $"[[Link|Count = {this.Parameters.Count}]]";
		#endregion
	}
}

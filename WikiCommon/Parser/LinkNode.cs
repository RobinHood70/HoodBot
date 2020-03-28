namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using static RobinHood70.CommonCode.Globals;
	/// <summary>Represents a link, including embedded images.</summary>
	public class LinkNode : IWikiNode, IBacklinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public LinkNode(IEnumerable<IWikiNode> title, IEnumerable<ParameterNode> parameters)
		{
			this.Title = new NodeCollection(this, title ?? throw ArgumentNull(nameof(title)));
			this.Parameters = new List<ParameterNode>(parameters ?? throw ArgumentNull(nameof(parameters)));
		}
		#endregion

		#region Public Properties

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
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

		/// <summary>Gets the parameters.</summary>
		/// <value>The parameters.</value>
		public IList<ParameterNode> Parameters { get; }

		/// <summary>Gets the title.</summary>
		/// <value>The title.</value>
		public NodeCollection Title { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new LinkNode from the provided text.</summary>
		/// <param name="text">The text of the link, optionally including surrounding brackets (<c>[[...]]</c>).</param>
		/// <returns>A new LinkNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c>, or any variant thereof).</exception>
		public static LinkNode FromText([Localizable(false)] string text) => WikiTextParser.SingleNode<LinkNode>(text);

		/// <summary>Creates a new LinkNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static LinkNode FromParts(string title) => FromParts(title, null as IEnumerable<string>);

		/// <summary>Creates a new LinkNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="displayText">The display text for the link.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static LinkNode FromParts(string title, string displayText) => FromParts(title, new[] { displayText });

		/// <summary>Creates a new LinkNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The default value.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static LinkNode FromParts(string title, IEnumerable<string>? parameters)
		{
			ThrowNull(title, nameof(title));
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
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Parameters.Count == 0 ? "[[Link]]" : $"[[Link|Count = {this.Parameters.Count}]]";
		#endregion
	}
}

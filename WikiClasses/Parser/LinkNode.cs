namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using RobinHood70.WikiClasses.Properties;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a link, including embedded images.</summary>
	public class LinkNode : IWikiNode, IBacklinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
		/// <param name="fullLink">The full link.</param>
		public LinkNode(string fullLink)
		{
			var newLink = ParseFullLink(fullLink);
			this.Title = new NodeCollection(this, newLink.Title);
			this.Parameters = new List<ParameterNode>();
			foreach (var param in newLink.Parameters)
			{
				this.Parameters.Add(new ParameterNode(param));
			}
		}

		/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="displayText">The display text.</param>
		public LinkNode(string title, string displayText)
			: this(title, new[] { displayText })
		{
		}

		/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public LinkNode(string title, IEnumerable<string> parameters)
		{
			ThrowNull(title, nameof(title));
			ThrowNull(parameters, nameof(parameters));
			var linkText = "[[" + title;
			var paramText = string.Join("|", parameters);
			if (paramText.Length > 0)
			{
				linkText += "|" + paramText;
			}

			linkText += "]]";
			var newLink = ParseFullLink(linkText);
			this.Title = newLink.Title;
			this.Parameters = newLink.Parameters;
		}

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

		#region Private Methods
		private static LinkNode ParseFullLink(string linkText)
		{
			var allNodes = WikiTextParser.Parse(linkText);
			if (allNodes.Count == 1 && allNodes.First?.Value is LinkNode retval)
			{
				return retval;
			}

			throw new InvalidOperationException(CurrentCulture(Resources.MalformedLink));
		}
		#endregion
	}
}

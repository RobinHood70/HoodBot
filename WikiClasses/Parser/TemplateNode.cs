namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a template call.</summary>
	public class TemplateNode : IWikiNode, IBacklinkNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TemplateNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		public TemplateNode(IEnumerable<IWikiNode> title, IList<ParameterNode> parameters)
		{
			this.Title = new NodeCollection(this, title ?? throw ArgumentNull(nameof(title)));
			this.Parameters = parameters ?? new List<ParameterNode>();
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the parameters.</summary>
		/// <value>The parameters.</value>
		public IList<ParameterNode> Parameters { get; }

		/// <summary>Gets the template name.</summary>
		/// <value>The template name.</value>
		public NodeCollection Title { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new TemplateNode from the provided text.</summary>
		/// <param name="txt">The text of the template, including surrounding braces (<c>{{...}}</c>).</param>
		/// <returns>A new TemplateNode.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c>, or any variant thereof).</exception>
		public static TemplateNode FromText(string txt) => WikiTextParser.SingleNode<TemplateNode>(txt);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static TemplateNode FromParts(string title) => FromParts(title, null);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The default value.</param>
		/// <returns>A new ArgumentNode.</returns>
		public static TemplateNode FromParts(string title, IEnumerable<string>? parameters)
		{
			ThrowNull(title, nameof(title));
			var templateText = "{{" + title;
			if (parameters != null)
			{
				var paramText = string.Join("|", parameters);
				if (paramText.Length > 0)
				{
					templateText += "|" + paramText;
				}
			}

			templateText += "}}";

			return FromText(templateText);
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

		/// <summary>Converts all parameter names to their corresponding text with values remaining as <see cref="NodeCollection"/>s.</summary>
		/// <returns>A dictionary of names and values.</returns>
		public Dictionary<string, NodeCollection> ParameterDictionary()
		{
			// TODO: Parameter-based methods are very primitive for now, just to get the basics working. Needs more work.
			var retval = new Dictionary<string, NodeCollection>();
			foreach (var parameter in this.Parameters)
			{
				retval.Add(parameter.Anonymous ? parameter.Index.ToString() : WikiTextVisitor.Value(parameter.Name!), parameter.Value);
			}

			return retval;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => this.Parameters.Count == 0 ? "{{Template}}" : $"{{Template|Count = {this.Parameters.Count}}}";
		#endregion

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}
}
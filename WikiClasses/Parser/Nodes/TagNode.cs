namespace RobinHood70.WikiClasses.Parser.Nodes
{
	public class TagNode : INodeBase
	{
		#region Constructors
		public TagNode(string name, string attributes, string innerText, string close)
		{
			this.Name = name;
			this.Attributes = attributes;
			this.InnerText = innerText;
			this.Close = close;
		}

		public string Attributes { get; set; }

		public string Close { get; set; } // Note that this is a full close tag, including the surrounding </...>.

		public string InnerText { get; set; }

		public string Name { get; set; } // Note that this is NOT a full open tag, it's just the name.

		public bool SelfClosed => this.InnerText?.Length == 0 && this.Close?.Length == 0;
		#endregion

		#region Public Methods
		public void Accept(IVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods
		public override string ToString() => this.SelfClosed ? $"<{this.Name}{this.Attributes}>" : $"<{this.Name}{this.Attributes}>{this.InnerText}{this.Close}";
		#endregion
	}
}

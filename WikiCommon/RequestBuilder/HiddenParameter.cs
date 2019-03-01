namespace RobinHood70.WikiCommon.RequestBuilder
{
	/// <summary>Represents a string parameter whose results should not be displayed to the user during debugging sessions and the like (e.g., passwords or tokens).</summary>
	/// <seealso cref="StringParameter" />
	public class HiddenParameter : StringParameter
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="HiddenParameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		public HiddenParameter(string name, string value)
			: base(name, value)
		{
		}
		#endregion

		#region Public Override Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
		public override void Accept(IParameterVisitor visitor) => visitor?.Visit(this);
		#endregion
	}
}

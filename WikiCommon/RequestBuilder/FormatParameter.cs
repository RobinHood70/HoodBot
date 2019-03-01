namespace RobinHood70.WikiCommon.RequestBuilder
{
	/// <summary>Represents the format parameter.</summary>
	/// <seealso cref="StringParameter" />
	/// <remarks>The only effect of this class is to change displayed links to use the pretty-print version of the format parameter, while leaving the actual requests sent to the server unaltered.</remarks>
	public class FormatParameter : StringParameter
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FormatParameter" /> class.</summary>
		/// <param name="value">The value.</param>
		public FormatParameter(string value)
			: base("format", value)
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

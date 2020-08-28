namespace RobinHood70.WikiCommon.RequestBuilder
{
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Represents a string parameter.</summary>
	/// <seealso cref="Parameter" />
	public class StringParameter : Parameter
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="StringParameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <remarks><see langword="null"/> is a valid value for this parameter type, so no input validation is performed.</remarks>
		public StringParameter(string name, string? value)
			: base(name ?? throw ArgumentNull(nameof(name))) => this.Value = value ?? string.Empty;
		#endregion

		#region Public Properties

		/// <summary>Gets the value of the parameter.</summary>
		public string Value { get; }
		#endregion

		#region Public Override Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
		public override void Accept(IParameterVisitor visitor)
		{
			ThrowNull(visitor, nameof(visitor));
			visitor.Visit(this);
		}

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name + "=" + this.Value;
		#endregion
	}
}

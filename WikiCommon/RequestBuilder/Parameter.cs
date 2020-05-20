namespace RobinHood70.WikiCommon.RequestBuilder
{
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Base class which represents a parameter with a generic value.</summary>
	/// <typeparam name="T">The type of the parameter the class will represent.</typeparam>
	/// <seealso cref="IParameter" />
	public abstract class Parameter<T> : IParameter
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Parameter{T}" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <remarks>The <paramref name="name"/> cannot be null, but the <paramref name="value"/> is left to the derived class to validate.</remarks>
		protected Parameter(string name, T value)
		{
			ThrowNull(name, nameof(name));
			this.Name = name;
			this.Value = value;
		}
		#endregion

		#region IParameter Properties

		/// <summary>Gets the name of the parameter.</summary>
		/// <value>The name of the parameter.</value>
		public string Name { get; }
		#endregion

		#region Public Properties

		/// <summary>Gets the value of the parameter.</summary>
		/// <value>The value of the parameter.</value>
		public T Value { get; }
		#endregion

		#region IParameter Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
		public abstract void Accept(IParameterVisitor visitor);
		#endregion
	}
}

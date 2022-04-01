namespace RobinHood70.WikiCommon.RequestBuilder
{
	using RobinHood70.CommonCode;

	/// <summary>Base class which represents a parameter with a generic value.</summary>
	/// <seealso cref="Parameter" />
	public abstract class Parameter
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Parameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		protected Parameter(string name)
		{
			this.Name = name.NotNull();
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the name of the parameter.</summary>
		/// <value>The name of the parameter.</value>
		public string Name { get; }
		#endregion

		#region Public Abstract Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
		public abstract void Accept(IParameterVisitor visitor);
		#endregion
	}
}

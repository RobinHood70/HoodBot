namespace RobinHood70.WikiCommon.RequestBuilder
{
	/// <summary>Specifies the properties and methods required by all parameter implementations.</summary>
	public interface IParameter
	{
		#region Properties

		/// <summary>Gets the name of the parameter.</summary>
		/// <value>The name of the parameter.</value>
		string Name { get; }
		#endregion

		#region Public Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
		void Accept(IParameterVisitor visitor);
		#endregion
	}
}

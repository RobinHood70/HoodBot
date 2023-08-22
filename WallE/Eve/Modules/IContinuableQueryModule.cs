namespace RobinHood70.WallE.Eve.Modules
{
	/// <summary>Specifies the set of properties and methods required by continuable query module implementations.</summary>
	/// <seealso cref="IQueryModule" />
	public interface IContinuableQueryModule : IQueryModule
	{
		#region Properties

		/// <summary>Gets the name of the continue parameter for the module.</summary>
		/// <value>The name of the continue parameter for the module.</value>
		string ContinueName { get; }

		/// <summary>Gets a value indicating whether the module is allowed to continue parsing results.</summary>
		/// <value><see langword="true" /> if parsing should continue; otherwise, <see langword="false" />.</value>
		bool ContinueParsing { get; }

		/// <summary>Gets or sets the upper limit of items this module can request.</summary>
		/// <value>The limit of items this module can request.</value>
		int ModuleLimit { get; set; }
		#endregion
	}
}
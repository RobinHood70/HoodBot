namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RequestBuilder;

	/// <summary>Specifies the set of properties and methods required by all query module implementations.</summary>
	/// <seealso cref="IModule" />
	public interface IQueryModule : IModule
	{
		#region Public Properties

		/// <summary>Gets a value indicating whether the module is allowed to continue parsing results.</summary>
		/// <value><see langword="true" /> if parsing should continue; otherwise, <see langword="false" />.</value>
		bool ContinueParsing { get; }

		/// <summary>Gets or sets the upper limit of items this module can request.</summary>
		/// <value>The limit of items this module can request.</value>
		int ModuleLimit { get; set; }

		/// <summary>Gets or sets a value indicating whether this instance is generator.</summary>
		/// <value><see langword="true" /> if this instance is generator; otherwise, <see langword="false" />.</value>
		bool IsGenerator { get; set; }
		#endregion

		#region Methods

		/// <summary>Builds this module's portion of the request.</summary>
		/// <param name="request">The request.</param>
		void BuildRequest(Request request);

		/// <summary>Deserializes the specified JSON into a concrete result.</summary>
		/// <param name="parent">The JSON to deserialize.</param>
		/// <remarks>Unlike the IActionModule equivalent, this does <i>not</i> create a conrete result in its own right due to the fact that the result may be parsed over the course of several requests before it's complete.</remarks>
		void Deserialize(JToken parent);

		/// <summary>Handles any warnings returned by the wiki.</summary>
		/// <param name="from">Where the warning originated.</param>
		/// <param name="text">The text of the warning.</param>
		/// <returns><see langword="true" /> if the warning was handled and does not need to be dealt with in any other way; <see langword="false" /> if it remains unhandled.</returns>
		bool HandleWarning(string from, string text);
		#endregion
	}
}

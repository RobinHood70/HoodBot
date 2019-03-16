namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;

	/// <summary>Specifies the set of properties and methods required by all query module implementations.</summary>
	/// <seealso cref="IModule" />
	public interface IQueryModule : IModule
	{
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

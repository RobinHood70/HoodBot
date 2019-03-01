namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;

	/// <summary>Specifies the set of properties and methods required by all action module implementations.</summary>
	/// <typeparam name="TInput">The type of the input.</typeparam>
	/// <typeparam name="TOutput">The type of the output.</typeparam>
	public interface IActionModule<TInput, TOutput> : IModule
	{
		#region Methods

		/// <summary>Creates a request to send to the wiki.</summary>
		/// <param name="input">The input.</param>
		/// <returns>A complete Request object that can be submitted to the MediaWiki API.</returns>
		Request CreateRequest(TInput input);

		/// <summary>Deserializes the specified JSON into an object.</summary>
		/// <param name="parent">The parent.</param>
		/// <returns>The output object.</returns>
		TOutput Deserialize(JToken parent);
		#endregion
	}
}

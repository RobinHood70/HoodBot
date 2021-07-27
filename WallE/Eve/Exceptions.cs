namespace RobinHood70.WallE.Eve
{
	using System.Runtime.CompilerServices;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Generic set of exceptions used across multiple modules.</summary>
	public static class Exceptions
	{
		#region Public Methods

		/// <summary>
		/// Malformeds the exception.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="token">The token with the bad data.</param>
		/// <param name="caller">The caller name (automatically populated).</param>
		/// <returns>RobinHood70.WallE.Design.WikiException.</returns>
		// These methods are not extensions, but are placed in this class as useful but not warranting a class of their own yet.
		public static WikiException MalformedException(string name, JToken? token, [CallerMemberName] string caller = FallbackText.Unknown) => new(CurrentCulture(EveMessages.MalformedData, name, token?.Path ?? FallbackText.Unknown, caller));

		/// <summary>
		/// Malformeds the type exception.
		/// </summary>
		/// <param name="typeName">Name of the type.</param>
		/// <param name="token">The token.</param>
		/// <param name="caller">The caller name (automatically populated).</param>
		/// <returns>RobinHood70.WallE.Design.WikiException.</returns>
		public static WikiException MalformedTypeException(string typeName, JToken? token, [CallerMemberName] string caller = FallbackText.Unknown) => new(CurrentCulture(EveMessages.MalformedDataType, typeName, token?.Path ?? FallbackText.Unknown, caller));
		#endregion
	}
}

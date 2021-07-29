namespace RobinHood70.WallE.Eve
{
	using System.Runtime.CompilerServices;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;

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
		public static WikiException MalformedException(string name, JToken? token, [CallerMemberName] string caller = Globals.Unknown) => new(Globals.CurrentCulture(EveMessages.MalformedData, name, token?.Path ?? Globals.Unknown, caller));

		/// <summary>Creates a new WikiException with a message indicating malformed data.</summary>
		/// <param name="typeName">Name of the expected type.</param>
		/// <param name="token">The token.</param>
		/// <param name="caller">The caller name (automatically populated).</param>
		/// <returns>A new <see cref="WikiException"/>.</returns>
		public static WikiException MalformedTypeException(string typeName, JToken? token, [CallerMemberName] string caller = Globals.Unknown) => new(Globals.CurrentCulture(EveMessages.MalformedDataType, typeName, token?.Path ?? Globals.Unknown, caller));
		#endregion
	}
}

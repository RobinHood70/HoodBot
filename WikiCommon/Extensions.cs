namespace RobinHood70.WikiCommon
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;

	/// <summary>Extension methods for a variety of types.</summary>
	public static class Extensions
	{
		#region DateTime Extensions

		/// <summary>Formats a <see cref="DateTime"/> in the standard MediaWiki format.</summary>
		/// <param name="timestamp">The timestamp to format.</param>
		/// <returns>A string with the date in the standard MediaWiki format.</returns>
		public static string ToMediaWiki(this DateTime timestamp) => timestamp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK", CultureInfo.InvariantCulture);

		/// <summary>Formats a <see cref="DateTime"/>? in the standard MediaWiki format.</summary>
		/// <param name="timestamp">The timestamp to format.</param>
		/// <returns>A string with the date in the standard MediaWiki format or <see langword="null"/> if the input value was null.</returns>
		[return: NotNullIfNotNull("timestamp")]
		public static string? ToMediaWiki(this DateTime? timestamp) => timestamp == null ? null : ToMediaWiki(timestamp.Value);
		#endregion
	}
}
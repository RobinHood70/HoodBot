namespace RobinHood70.WallE.Clients
{
	using System;
	using System.Globalization;
	using System.Reflection;

	/// <summary>Shared classes that apply to multiple client classes.</summary>
	public static class ClientShared
	{
		#region Constants

		/// <summary>A constant for the form URL encoded mime type.</summary>
		public const string FormUrlEncoded = "application/x-www-form-urlencoded";
		#endregion

		/// <summary>Builds a user agent value the conforms to MediaWiki's recommended spec: https://meta.wikimedia.org/wiki/User-Agent_policy.</summary>
		/// <param name="contactInfo">E-mail address or username on the wiki you will be editing on.</param>
		/// <returns>The user agent string.</returns>
		public static string BuildUserAgent(string? contactInfo)
		{
			// This routine is deliberately not localized.
			string botInfo;
			var libraryName = Assembly.GetExecutingAssembly().GetName();
			var currentAssembly = Assembly.GetEntryAssembly();
			if (currentAssembly == null)
			{
				botInfo = "unknown";
			}
			else
			{
				botInfo = currentAssembly.GetName().Name ?? "unknown";
				if (currentAssembly.GetName().Version is Version currentVersion)
				{
					botInfo += '/' + currentVersion.ToString();
				}
			}

			contactInfo = string.IsNullOrWhiteSpace(contactInfo)
				? string.Empty
				: $" ({contactInfo.Trim()})";

			return string.Create(
				CultureInfo.InvariantCulture,
				$"{botInfo}{contactInfo} {libraryName.Name}/{libraryName.Version}");
		}
	}
}

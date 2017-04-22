namespace RobinHood70.WallE.Eve
{
	using System;
	using System.Collections.Generic;
	using static Properties.EveMessages;

	internal static class TokenManagerFunctions
	{
		#region Public Static Methods
		public static string TrimToken(string token) => (token?.EndsWith("token", StringComparison.Ordinal) ?? false) ? token.Substring(0, token.Length - 5) : token;

		public static string ValidateTokenType(HashSet<string> validTypes, string type, string replace, string replaceWith)
		{
			if (type == replace)
			{
				type = replaceWith;
			}

			if (!validTypes.Contains(type))
			{
				throw new ArgumentException(BadTokenRequest, nameof(type));
			}

			return type;
		}
		#endregion
	}
}

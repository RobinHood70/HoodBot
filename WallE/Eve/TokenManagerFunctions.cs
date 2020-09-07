﻿namespace RobinHood70.WallE.Eve
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Properties;
	using static RobinHood70.CommonCode.Globals;

	internal static class TokenManagerFunctions
	{
		#region Public Static Methods
		public static string TrimTokenKey(string key) =>
			key == null ? throw ArgumentNull(nameof(key)) :
			key.EndsWith("token", StringComparison.Ordinal) ? key[0..^5] :
			key;

		public static string ValidateTokenType(HashSet<string> validTypes, string type, string replace, string replaceWith)
		{
			if (string.Equals(type, replace, StringComparison.Ordinal))
			{
				type = replaceWith;
			}

			return !validTypes.Contains(type) ? throw new ArgumentException(EveMessages.BadTokenRequest, nameof(type)) : type;
		}
		#endregion
	}
}

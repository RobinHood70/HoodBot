﻿namespace RobinHood70.WallE.Eve;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Properties;

internal static class TokenManagerFunctions
{
	#region Public Static Methods
	public static string TrimTokenKey(string key)
	{
		ArgumentNullException.ThrowIfNull(key);
		return key.EndsWith("token", StringComparison.Ordinal)
			? key[0..^5]
			: key;
	}

	public static string ValidateTokenType(HashSet<string> validTypes, string type, string replace, string replaceWith)
	{
		if (type.OrdinalEquals(replace))
		{
			type = replaceWith;
		}

		return !validTypes.Contains(type) ? throw new ArgumentException(paramName: nameof(type), message: EveMessages.BadTokenRequest) : type;
	}
	#endregion
}
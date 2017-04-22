namespace RobinHood70.WallE
{
	using System;
	using System.Globalization;
	using System.Text;

	#region Internal Enumerations
	internal enum HashType
	{
		Md5,
		Sha1
	}
	#endregion

	internal static class Extensions
	{
		#region string Extensions
		public static string PackHex(this string hexValue)
		{
			if (hexValue == null)
			{
				return null;
			}

			if ((hexValue.Length & 1) == 1)
			{
				hexValue = "0" + hexValue;
			}

			var hexLength2 = hexValue.Length >> 1;
			var retval = new byte[hexLength2];
			for (var i = 0; i < hexLength2; i++)
			{
				retval[i] = Convert.ToByte(hexValue.Substring(i << 1, 2), 16);
			}

			return Encoding.UTF8.GetString(retval);
		}
		#endregion

		#region DateTime Extensions
		public static string ToMediaWiki(this DateTime timestamp) => timestamp.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK", CultureInfo.InvariantCulture);

		public static string ToMediaWiki(this DateTime? timestamp) => timestamp == null ? null : ToMediaWiki(timestamp.Value);
		#endregion
	}
}

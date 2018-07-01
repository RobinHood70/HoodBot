namespace RobinHood70.HoodBot
{
	using System;
	using System.IO;

	public static class Globals
	{
		public static string ApplicationDataPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(HoodBot));

		public static string ContactInfo { get; set; }

		public static string CookiesLocation => ApplicationDataPath == null ? null : Path.Combine(ApplicationDataPath, "Cookies.dat");

		public static string WikiListLocation => ApplicationDataPath == null ? null : Path.Combine(ApplicationDataPath, "WikiList.json");
	}
}

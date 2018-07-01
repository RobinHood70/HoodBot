namespace RobinHood70.HoodBot
{
	using System.IO;
	using static System.Environment;

	public static class Globals
	{
		public static string ApplicationDataPath { get; } = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.Create), nameof(HoodBot));

		public static string ContactInfo { get; set; }

		public static string CookiesLocation => Path.Combine(ApplicationDataPath, "Cookies.dat");

		public static string WikiListLocation => Path.Combine(ApplicationDataPath, "WikiList.json");
	}
}

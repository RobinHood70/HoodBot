namespace RobinHood70.HoodBot
{
	using System.Globalization;
	using System.Threading;
	using System.Windows;

	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e) => Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-CA");
	}
}

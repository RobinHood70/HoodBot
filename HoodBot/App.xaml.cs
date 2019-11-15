namespace RobinHood70.HoodBot
{
	using System;
	using System.Globalization;
	using System.Threading;
	using System.Windows;
	using System.Windows.Threading;

	/// <summary>Interaction logic for App.xaml.</summary>
	public partial class App : Application
	{
		// Ensure everything is updated on the UI end before continuing.
		public static void WpfYield() => Current.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);

		private void Application_Startup(object sender, StartupEventArgs e) => Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-CA");
	}
}
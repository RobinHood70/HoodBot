namespace RobinHood70.HoodBot
{
	using System;
	using System.Windows;
	using System.Windows.Threading;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using RobinHood70.HoodBot.ViewModels;
	using RobinHood70.HoodBot.Views;

	/// <summary>Interaction logic for App.xaml.</summary>
	public partial class App : Application
	{
		#region Static Fields
		// Although this is IDisposable, we don't implement IDisposable here, instead using the OnStartup and OnExit methods to effectively do the same thing.
		private static IHost host { get; } = Host
				.CreateDefaultBuilder()
				.ConfigureAppConfiguration((context, builder) => builder
					.AddJsonFile("appsettings.json", true, false)
					.AddJsonFile("connectionStrings.json", false, false))
				.ConfigureServices((context, services) => ConfigureServices(context.Configuration, services))
				.Build();
		#endregion

		#region Public Properties
		public static ViewModelLocator Locator => ViewModelLocator.Instance;

		public static IServiceProvider ServiceProvider { get; } = host.Services;
		#endregion

		#region Public Static Methods

		// Ensure everything is updated on the UI end before continuing.
		public static void WpfYield() => Current.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
		#endregion

		#region Protected Override Methods
		protected override async void OnExit(ExitEventArgs e)
		{
			await host.StopAsync(TimeSpan.FromSeconds(5));
			host.Dispose();
			base.OnExit(e);
		}

		protected override async void OnStartup(StartupEventArgs e)
		{
			await host.StartAsync();
			base.OnStartup(e);
			host.Services.GetRequiredService<MainWindow>().Show();
		}
		#endregion

		#region Private Methods
		private static void ConfigureServices(IConfiguration configuration, IServiceCollection services) => services
			.Configure<AppSettings>(configuration.GetSection(nameof(AppSettings)))
			.AddSingleton<MainViewModel>()
			.AddSingleton<MainWindow>()
			.AddTransient<SettingsViewModel>()
			.AddTransient<SettingsWindow>()
			;
		#endregion
	}
}
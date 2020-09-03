namespace RobinHood70.HoodBot
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Threading;
	using Microsoft.Extensions.Configuration;
	using Microsoft.Extensions.DependencyInjection;
	using Microsoft.Extensions.Hosting;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.ViewModels;
	using RobinHood70.HoodBot.Views;
	using static System.Environment;

	/// <summary>Interaction logic for App.xaml.</summary>
	public partial class App : Application
	{
		#region Static Fields
		// Although this is IDisposable, we don't implement IDisposable here, instead using the OnStartup and OnExit methods to effectively do the same thing.
		private static readonly IHost AppHost = Host
				.CreateDefaultBuilder()
				.ConfigureServices((context, services) => ConfigureServices(context.Configuration, services))
				.Build();
		#endregion

		#region Public Static Properties
		public static string AppFolder { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

		public static AppSettings AppSettings { get; } = Settings.Load<AppSettings>();

		public static ViewModelLocator Locator => ViewModelLocator.Instance;

		public static IServiceProvider ServiceProvider { get; } = AppHost.Services;

		public static string UserFolder { get; } = Path.Combine(GetFolderPath(SpecialFolder.ApplicationData, SpecialFolderOption.Create), nameof(HoodBot));

		public static UserSettings UserSettings { get; } = Settings.Load<UserSettings>();
		#endregion

		#region Public Static Methods

		// Ensure everything is updated on the UI end before continuing.
		public static void WpfYield() => Current.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);
		#endregion

		#region Protected Override Methods
		protected override async void OnExit(ExitEventArgs e)
		{
			await AppHost.StopAsync(TimeSpan.FromSeconds(5));
			AppHost.Dispose();
			Settings.Save(UserSettings);
			base.OnExit(e);
		}

		protected override async void OnStartup(StartupEventArgs e)
		{
			await AppHost.StartAsync();
			base.OnStartup(e);
			AppHost.Services.GetRequiredService<MainWindow>().Show();
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
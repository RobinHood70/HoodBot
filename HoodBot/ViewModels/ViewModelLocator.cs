namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using Microsoft.Extensions.DependencyInjection;
	using RobinHood70.HoodBot.Views;

	public class ViewModelLocator
	{
		private readonly IServiceProvider serviceProvider = App.ServiceProvider;

		public static ViewModelLocator Instance = new ViewModelLocator();

		public MainViewModel MainViewModel => this.serviceProvider.GetRequiredService<MainViewModel>();

		public MainWindow MainWindow => this.serviceProvider.GetRequiredService<MainWindow>();

		public SettingsViewModel SettingsViewModel => this.serviceProvider.GetRequiredService<SettingsViewModel>();

		public SettingsWindow SettingsWindow => this.serviceProvider.GetRequiredService<SettingsWindow>();

	}
}

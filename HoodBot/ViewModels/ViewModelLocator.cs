namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using Microsoft.Extensions.DependencyInjection;
	using RobinHood70.HoodBot.Views;

	public class ViewModelLocator
	{
		#region Private Fields
		private readonly IServiceProvider serviceProvider = App.ServiceProvider;
		#endregion

		#region Public Static Properties
		public static ViewModelLocator Instance { get; } = new ViewModelLocator();
		#endregion

		#region Public Properties
		public MainViewModel MainViewModel => this.serviceProvider.GetRequiredService<MainViewModel>();

		public MainWindow MainWindow => this.serviceProvider.GetRequiredService<MainWindow>();

		public SettingsViewModel SettingsViewModel => this.serviceProvider.GetRequiredService<SettingsViewModel>();

		public SettingsWindow SettingsWindow => this.serviceProvider.GetRequiredService<SettingsWindow>();
		#endregion
	}
}

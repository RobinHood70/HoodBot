namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Windows.Input;

	public class RelayCommand<T> : ICommand
	{
		private readonly Action<T> action;
		private readonly Predicate<T>? canExecute;

		public RelayCommand(Action<T> action) => this.action = action;

		public RelayCommand(Action<T> action, Predicate<T> canExecute)
			: this(action) => this.canExecute = canExecute;

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute([AllowNull] T parameter) => this.canExecute == null || this.canExecute(parameter!);

		public bool CanExecute(object? parameter) => this.CanExecute(parameter is null ? default : (T)parameter);

		public void Execute([AllowNull] T parameter) => this.action.Invoke(parameter!);

		public void Execute(object? parameter) => this.Execute(parameter is null ? default : (T)parameter);
	}
}
namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Windows.Input;

	public class RelayCommand : ICommand
	{
		private readonly Action action;
		private readonly Func<bool> canExecute;

		public RelayCommand(Action action) => this.action = action;

		public RelayCommand(Action action, Func<bool> canExecute)
			: this(action) => this.canExecute = canExecute;

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Renamed to indicate that parameter is ignored.")]
		public bool CanExecute(object ignored) => this.canExecute == null ? true : this.canExecute();

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Renamed to indicate that parameter is ignored.")]
		public void Execute(object ignored) => this.action();
	}
}
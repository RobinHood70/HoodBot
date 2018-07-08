namespace RobinHood70.HoodBot
{
	using System.Windows;
	using RobinHood70.HoodBot.ViewModel;

	/// <summary>Interaction logic for MainWindow.xaml.</summary>
	public partial class MainWindow : Window
	{
		public MainWindow() => this.InitializeComponent();

		private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			// Less than ideal to do it this way, but avoids additional dependencies. Simply routes back into VM.
			var vm = this.DataContext as MainViewModel;
			vm.GetParameters(e.NewValue as JobNode);
		}
	}
}

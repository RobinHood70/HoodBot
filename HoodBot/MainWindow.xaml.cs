namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections;
	using System.Diagnostics;
	using System.Windows;
	using System.Windows.Controls;
	using RobinHood70.HoodBot.ViewModel;

	/// <summary>Interaction logic for MainWindow.xaml.</summary>
	public partial class MainWindow : Window, IParameterFetcher
	{
		public MainWindow()
		{
			this.InitializeComponent();
			var vm = this.DataContext as MainViewModel;
			vm.ParameterFetcher = this;
		}

		public void GetParameter(ConstructorParameter parameter)
		{
			var name = parameter.Name;
			var label = parameter.Label;
			var valueType = parameter.Type;
			var value = parameter.Value;
			Debug.WriteLine($"Got request for parameter {label} of type {valueType} with current value of {value}.");
			var stackPanel = this.JobParameters.Children;
			stackPanel.Clear();
			var dockPanel = new DockPanel();
			stackPanel.Add(dockPanel);

			var controls = dockPanel.Children;
			controls.Add(new TextBlock() { Text = parameter.Label + (valueType == typeof(bool) ? '?' : ':') });

			UIElement controlToAdd = null;
			if (valueType == typeof(bool))
			{
				controlToAdd = new CheckBox() { IsChecked = (bool)value };
			}
			else if (typeof(IFormattable).IsAssignableFrom(valueType))
			{
				controlToAdd = new TextBox() { Text = (value as IFormattable).ToString() };
			}
			else if (typeof(IEnumerable).IsAssignableFrom(valueType))
			{
				controlToAdd = new TextBox() { Text = string.Join(Environment.NewLine, value), AcceptsReturn = true };
			}
			else
			{
				throw new NotSupportedException($"Here we are, trying to handle a {valueType.Name}!");
			}

			controlToAdd.SetValue(NameProperty, name);
			this.RegisterName(name, controlToAdd);
			controls.Add(controlToAdd);
		}

		public void SetParameter(ConstructorParameter parameter)
		{
			var valueType = parameter.Type;
			var control = this.JobParameters.FindName(parameter.Name);
			if (valueType == typeof(bool))
			{
				parameter.Value = (control as CheckBox).IsChecked;
			}
			else
			{
				var text = (control as TextBox).Text;
				if (valueType == typeof(string))
				{
					parameter.Value = text;
				}
				else if (valueType == typeof(int))
				{
					if (int.TryParse(text, out var result))
					{
						parameter.Value = result;
					}
				}
				else if (typeof(IEnumerable).IsAssignableFrom(valueType))
				{
					parameter.Value = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
				}
				else
				{
					throw new NotSupportedException($"Here we are, trying to handle a {valueType.Name}!");
				}
			}
		}

		private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			// TODO: Consider changing to attached property to be fully MVVM compliant.
			var tv = sender as TreeView;
			var vm = tv.DataContext as MainViewModel;
			vm.SetParameters(e.OldValue as JobNode);
			vm.GetParameters(e.NewValue as JobNode);
		}

		private void TreeView_Checked(object sender, RoutedEventArgs e)
		{
			// TODO: Consider changing to attached property to be fully MVVM compliant.
			var tv = sender as TreeView;
			var item = (e.OriginalSource as CheckBox).DataContext as JobNode;
			if (tv.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvItem)
			{
				tvItem.IsSelected = true;
			}
		}

		private void JobParametersBorder_LostFocus(object sender, RoutedEventArgs e)
		{
			var tv = sender as Border;
			var vm = tv.DataContext as MainViewModel;
			var jobNode = this.SelectedJobs.SelectedItem as JobNode;
			vm.SetParameters(jobNode);
		}
	}
}

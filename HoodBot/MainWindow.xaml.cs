namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections;
	using System.Windows;
	using System.Windows.Controls;
	using RobinHood70.HoodBot.ViewModel;
	using static RobinHood70.WikiCommon.Globals;
	using static RobinHood70.HoodBot.Properties.Resources;

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
			ThrowNull(parameter, nameof(parameter));
			var valueType = parameter.Type;
			var grid = this.JobParameters;
			if (grid.RowDefinitions.Count > 0)
			{
				grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });
			}

			grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
			var lastRow = grid.RowDefinitions.Count - 1;

			var labelControl = new TextBlock() { Text = parameter.Label + (valueType == typeof(bool) ? '?' : ':') };
			Grid.SetColumn(labelControl, 0);
			Grid.SetRow(labelControl, lastRow);
			grid.Children.Add(labelControl);

			Control controlToAdd = null;
			if (valueType == typeof(bool))
			{
				controlToAdd = new CheckBox() { IsChecked = (bool)parameter.Value };
			}
			else if (typeof(IFormattable).IsAssignableFrom(valueType))
			{
				controlToAdd = new TextBox() { Text = (parameter.Value as IFormattable).ToString(), AcceptsReturn = false };
			}
			else if (typeof(IEnumerable).IsAssignableFrom(valueType))
			{
				var textValue = string.Empty;
				if (parameter.Value is IEnumerable parameterValues)
				{
					foreach (var value in parameterValues)
					{
						textValue += value.ToString();
					}
				}

				controlToAdd = new TextBox() { Text = textValue, AcceptsReturn = true };
				labelControl.VerticalAlignment = VerticalAlignment.Top;
			}
			else
			{
				throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, valueType.Name));
			}

			Grid.SetColumn(controlToAdd, 2);
			Grid.SetRow(controlToAdd, lastRow);
			controlToAdd.Name = parameter.Name;
			this.RegisterName(parameter.Name, controlToAdd);
			grid.Children.Add(controlToAdd);
		}

		public void SetParameter(ConstructorParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			var valueType = parameter.Type;
			var control = this.JobParameters.FindName(parameter.Name);
			if (control == null)
			{
				return;
			}

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
					throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, valueType.Name));
				}
			}
		}

		private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			// TODO: Consider changing to attached property to be fully MVVM compliant.
			this.JobParameters.Children.Clear();
			this.JobParameters.RowDefinitions.Clear();

			var tv = sender as TreeView;
			var vm = tv.DataContext as MainViewModel;
			var oldNode = e.OldValue as JobNode;
			if (oldNode != null)
			{
				vm.SetParameters(oldNode);
				foreach (var param in oldNode.Parameters)
				{
					this.UnregisterName(param.Name);
				}
			}

			vm.GetParameters(e.NewValue as JobNode);
		}

		private void TreeView_Checked(object sender, RoutedEventArgs e)
		{
			// TODO: Consider changing to attached property to be fully MVVM compliant.
			var tv = sender as TreeView;
			var item = (e.OriginalSource as FrameworkElement).DataContext as JobNode;
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

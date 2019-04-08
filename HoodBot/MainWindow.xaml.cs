namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections;
	using System.Windows;
	using System.Windows.Controls;
	using RobinHood70.HoodBot.ViewModel;
	using RobinHood70.WikiCommon;
	using static RobinHood70.HoodBot.Properties.Resources;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Interaction logic for MainWindow.xaml.</summary>
	public partial class MainWindow : Window, IParameterFetcher
	{
		#region Constructors
		public MainWindow()
		{
			this.InitializeComponent();
			var vm = this.DataContext as MainViewModel;
			vm.ParameterFetcher = this;
		}
		#endregion

		#region Public Methods
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
			Control controlToAdd;
			if (valueType == typeof(bool))
			{
				controlToAdd = new CheckBox() { IsChecked = (bool)parameter.Value };
			}
			//// else if (parameter.Attribute is JobParameterFileAttribute fileAttribute)
			//// {
			////	controlToAdd = new FileTextBox();
			//// }
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
					parameter.Value = text.Split(TextArrays.EnvironmentNewLine, StringSplitOptions.None);
				}
				else
				{
					throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, valueType.Name));
				}
			}
		}
		#endregion

		#region Private Static Methods

		// These next two methods adapted from https://social.msdn.microsoft.com/Forums/silverlight/en-US/84cd3a27-6b17-48e6-8f8a-e5737601fdac/treeviewitemcontainergeneratorcontainerfromitem-returns-null?forum=silverlightnet
		private static TreeViewItem ContainerFromItem(TreeView treeView, object item) =>
			(treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem)
			?? ContainerFromItem(treeView.ItemContainerGenerator, treeView.Items, item);

		private static TreeViewItem ContainerFromItem(ItemContainerGenerator generator, ItemCollection itemCollection, object item)
		{
			foreach (var curChildItem in itemCollection)
			{
				var parentContainer = generator.ContainerFromItem(curChildItem) as TreeViewItem;
				var newGenerator = parentContainer?.ItemContainerGenerator;
				if (newGenerator != null)
				{
					if (newGenerator.ContainerFromItem(item) is TreeViewItem retval)
					{
						return retval;
					}

					if (ContainerFromItem(newGenerator, parentContainer.Items, item) is TreeViewItem retval2)
					{
						return retval2;
					}
				}
			}

			return null;
		}
		#endregion

		#region Private Methods
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
				if (oldNode.Parameters != null)
				{
					foreach (var param in oldNode.Parameters)
					{
						this.UnregisterName(param.Name);
					}
				}
			}

			var newNode = e.NewValue as JobNode;
			if (newNode.Constructor != null)
			{
				vm.GetParameters(newNode);
			}
			else if (newNode.Children != null)
			{
				foreach (var childNode in newNode.Children)
				{
					vm.GetParameters(childNode);
				}
			}
		}

		private void TreeView_Checked(object sender, RoutedEventArgs e)
		{
			// TODO: Consider changing to attached property to be fully MVVM compliant.
			var jobNode = (e.OriginalSource as FrameworkElement).DataContext as JobNode;
			if (ContainerFromItem(sender as TreeView, jobNode) is TreeViewItem tvItem)
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
		#endregion
	}
}
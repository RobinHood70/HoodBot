namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections;
	using System.Windows;
	using System.Windows.Controls;
	using RobinHood70.CommonCode;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.HoodBot.Properties.Resources;

	public class MainWindowParameterFetcher : IParameterFetcher
	{
		#region Public Methods
		public void GetParameter(ConstructorParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			var main = App.Locator.MainWindow;
			var grid = main.JobParameters;
			var (label, input) = CreateControl(parameter);
			if (grid.RowDefinitions.Count > 0)
			{
				grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10) });
			}

			grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
			var lastRow = grid.RowDefinitions.Count - 1;

			Grid.SetColumn(label, 0);
			Grid.SetRow(label, lastRow);
			grid.Children.Add(label);

			Grid.SetColumn(input, 2);
			Grid.SetRow(input, lastRow);
			main.RegisterName(parameter.Name, input);
			grid.Children.Add(input);
		}

		public void SetParameter(ConstructorParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			if (App.Locator.MainWindow.JobParameters.FindName(parameter.Name) is Control control)
			{
				parameter.Value = GetControlValue(parameter.Type, control);
			}
		}
		#endregion

		#region Private Static Methods

		private static (TextBlock Label, Control Input) CreateControl(ConstructorParameter parameter)
		{
			var valueType = parameter.Type;
			var labelControl = new TextBlock() { Text = parameter.Label + (valueType == typeof(bool) ? '?' : ':') };

			Control controlToAdd;
			if (valueType == typeof(bool))
			{
				controlToAdd = new CheckBox() { IsChecked = (bool?)parameter.Value };
			}
			//// else if (parameter.Attribute is JobParameterFileAttribute fileAttribute)
			//// {
			////	controlToAdd = new FileTextBox();
			//// }
			else if (typeof(IFormattable).IsAssignableFrom(valueType))
			{
				controlToAdd = new TextBox() { Text = (parameter.Value as IFormattable)?.ToString(), AcceptsReturn = false };
			}
			else if (typeof(IEnumerable).IsAssignableFrom(valueType))
			{
				var textValue = string.Empty;
				if (parameter.Value is IEnumerable parameterValues)
				{
					foreach (var value in parameterValues)
					{
						textValue += value?.ToString();
					}
				}

				controlToAdd = new TextBox() { Text = textValue, AcceptsReturn = true };
				labelControl.VerticalAlignment = VerticalAlignment.Top;
			}
			else
			{
				throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, valueType.Name));
			}

			controlToAdd.Name = parameter.Name;
			return (labelControl, controlToAdd);
		}

		private static object? GetControlValue(Type expectedType, Control control) =>
			control is CheckBox checkBox ? (object)(checkBox.IsChecked == true) :
			control is TextBox textBox && textBox.Text is var text ?
				expectedType == typeof(string) ? text :
				expectedType == typeof(int) ? int.Parse(text) :
				typeof(IEnumerable).IsAssignableFrom(expectedType) ? (object)text.Split(TextArrays.EnvironmentNewLine, StringSplitOptions.None)
					: throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, expectedType.Name))
				: throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, expectedType.Name));
		#endregion
	}
}

namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections;
	using System.Globalization;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Views;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.HoodBot.Properties.Resources;

	public class MainWindowParameterFetcher : IParameterFetcher
	{
		#region Fields
		private readonly JobInfo job;
		private readonly MainWindow main;
		private readonly Grid jobParameters;
		#endregion

		#region Constructors
		public MainWindowParameterFetcher(JobInfo jobInfo)
		{
			this.job = jobInfo ?? throw ArgumentNull(nameof(jobInfo));
			this.main = App.Locator.MainWindow;
			this.jobParameters = this.main.JobParameters;
		}
		#endregion

		#region Public Methods
		public void ClearParameters()
		{
			foreach (var param in this.job.Parameters)
			{
				this.main.UnregisterName(param.Name);
			}
		}

		public void GetParameters()
		{
			// TODO: Consider changing to attached property to be fully MVVM compliant.
			this.jobParameters.Children.Clear();
			this.jobParameters.RowDefinitions.Clear();
			foreach (var param in this.job.Parameters)
			{
				this.GetParameter(param);
			}
		}

		public void SetParameters()
		{
			foreach (var param in this.job.Parameters)
			{
				this.SetParameter(param);
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
				StringBuilder sb = new();
				if (parameter.Value is IEnumerable parameterValues)
				{
					foreach (var value in parameterValues)
					{
						sb.Append(value);
					}
				}

				controlToAdd = new TextBox() { Text = sb.ToString(), AcceptsReturn = true };
				labelControl.VerticalAlignment = VerticalAlignment.Top;
			}
			else
			{
				throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, valueType.Name));
			}

			controlToAdd.Name = parameter.Name;
			return (labelControl, controlToAdd);
		}

		private static object? GetControlValue(Type expectedType, Control control)
		{
			if (control is CheckBox checkBox)
			{
				return checkBox.IsChecked == true;
			}

			if (control is TextBox textBox && textBox.Text is var text)
			{
				if (expectedType == typeof(string))
				{
					return text;
				}

				if (expectedType == typeof(int))
				{
					return int.Parse(text, CultureInfo.InvariantCulture);
				}

				if (typeof(IEnumerable).IsAssignableFrom(expectedType))
				{
					if (!text.Contains(Environment.NewLine, StringComparison.Ordinal))
					{
						text = text.Replace("\n", Environment.NewLine, StringComparison.Ordinal);
					}

					return text.Split(TextArrays.EnvironmentNewLine, StringSplitOptions.None);
				}
			}

			throw new NotSupportedException(CurrentCulture(UnhandledConstructorParameter, expectedType.Name));
		}
		#endregion

		#region Private Methods
		private void GetParameter(ConstructorParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			var grid = this.main.JobParameters;
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
			this.main.RegisterName(parameter.Name, input);
			grid.Children.Add(input);
		}

		private void SetParameter(ConstructorParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			var jobParams = this.main.JobParameters;
			var foundName = jobParams.FindName(parameter.Name);
			if (foundName is Control control)
			{
				parameter.Value = GetControlValue(parameter.Type, control);
			}
		}
		#endregion
	}
}

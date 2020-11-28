namespace RobinHood70.HoodBot.Controls
{
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Controls;

	/// <summary>Interaction logic for NumericUpDown.xaml.</summary>
	public partial class NumericUpDown : UserControl
	{
		#region Public Static Fields
		private static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(10000));
		private static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));
		private static readonly DependencyProperty StepValueProperty = DependencyProperty.Register("StepValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(100));
		private static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ValueChanged));
		#endregion

		#region Constructors
		public NumericUpDown()
		{
			this.InitializeComponent();
			this.Number.Text = "0";
		}
		#endregion

		#region Public Properties
		public int Maximum
		{
			get => (int)this.GetValue(MaximumProperty);
			set => this.SetValue(MaximumProperty, value);
		}

		public int Minimum
		{
			get => (int)this.GetValue(MinimumProperty);
			set => this.SetValue(MinimumProperty, value);
		}

		public int StepValue
		{
			get => (int)this.GetValue(StepValueProperty);
			set => this.SetValue(StepValueProperty, value);
		}

		[SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "Can't rename methods and property name is the most logical.")]
		public int Value
		{
			get => (int)this.GetValue(ValueProperty);
			set => this.SetValue(ValueProperty, value);
		}
		#endregion

		#region Private Static Methods
		private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var num = (int)e.NewValue;
			((NumericUpDown)d).Number.Text = num.ToString(CultureInfo.CurrentUICulture);
		}
		#endregion

		#region Private Methods
		private void Down_Click(object sender, RoutedEventArgs e)
		{
			var newValue = this.Value - this.StepValue;
			if (newValue >= this.Maximum)
			{
				newValue = this.Maximum;
			}

			this.Value = newValue;
		}

		private void Number_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!int.TryParse(this.Number.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
			{
				// Simple error handling for now; should probably limit input to digits and copy-paste/select/movement keys.
				this.Value = 0;
			}
			else
			{
				if (this.Value != value)
				{
					this.Value = value;
				}
			}
		}

		private void Up_Click(object sender, RoutedEventArgs e)
		{
			var newValue = this.Value + this.StepValue;
			if (newValue >= this.Maximum)
			{
				newValue = this.Maximum;
			}

			this.Value = newValue;
		}
		#endregion
	}
}
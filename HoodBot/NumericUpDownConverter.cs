namespace RobinHood70.HoodBot
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	public sealed class NumericUpDownConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (double)value / 1.5;

		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
	}
}

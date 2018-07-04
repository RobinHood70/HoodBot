namespace RobinHood70.HoodBot
{
	using System.Windows;
	using System.Windows.Controls;

	// Taken from https://www.wpftutorial.net/PasswordBox.html
	public static class PasswordHelper
	{
		#region Public Fields
		public static readonly DependencyProperty AttachProperty =
			DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, Attach));

		public static readonly DependencyProperty PasswordProperty =
			DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordHelper), new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));
		#endregion

		#region Private Fields
		private static readonly DependencyProperty IsUpdatingProperty =
			DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordHelper));
		#endregion

		#region Public Methods
		public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);

		public static string GetPassword(DependencyObject dp) => (string)dp.GetValue(PasswordProperty);

		public static void SetAttach(DependencyObject dp, bool value) => dp.SetValue(AttachProperty, value);

		public static void SetPassword(DependencyObject dp, string value) => dp.SetValue(PasswordProperty, value);
		#endregion

		#region Private Methods
		private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is PasswordBox passwordBox)
			{
				if ((bool)e.OldValue)
				{
					passwordBox.PasswordChanged -= PasswordChanged;
				}

				if ((bool)e.NewValue)
				{
					passwordBox.PasswordChanged += PasswordChanged;
				}
			}
		}

		private static bool GetIsUpdating(DependencyObject dp) => (bool)dp.GetValue(IsUpdatingProperty);

		private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var passwordBox = sender as PasswordBox;
			passwordBox.PasswordChanged -= PasswordChanged;

			if (!GetIsUpdating(passwordBox))
			{
				passwordBox.Password = (string)e.NewValue;
			}

			passwordBox.PasswordChanged += PasswordChanged;
		}

		private static void PasswordChanged(object sender, RoutedEventArgs e)
		{
			var passwordBox = sender as PasswordBox;
			SetIsUpdating(passwordBox, true);
			SetPassword(passwordBox, passwordBox.Password);
			SetIsUpdating(passwordBox, false);
		}

		private static void SetIsUpdating(DependencyObject dp, bool value) => dp.SetValue(IsUpdatingProperty, value);
		#endregion
	}
}

﻿namespace RobinHood70.HoodBot
{
	using System.Windows;
	using System.Windows.Controls;
	using RobinHood70.CommonCode;

	// Taken from https://www.wpftutorial.net/PasswordBox.html
	public static class PasswordHelper
	{
		#region Public Fields
		public static readonly DependencyProperty AttachProperty =
			DependencyProperty.RegisterAttached("Attach", typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, Attach));

		public static readonly DependencyProperty PasswordProperty =
			DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordHelper), new FrameworkPropertyMetadata(string.Empty, Password_Changed));
		#endregion

		#region Private Fields
		private static readonly DependencyProperty IsUpdatingProperty =
			DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordHelper));
		#endregion

		#region Public Methods
		public static bool GetAttach(DependencyObject dp) => (bool)dp.NotNull(nameof(dp)).GetValue(AttachProperty);

		public static string GetPassword(DependencyObject dp) => (string)dp.NotNull(nameof(dp)).GetValue(PasswordProperty);

		public static void SetAttach(DependencyObject dp, bool value) => dp.NotNull(nameof(dp)).SetValue(AttachProperty, value);

		public static void SetPassword(DependencyObject dp, string value) => dp.NotNull(nameof(dp)).SetValue(PasswordProperty, value);
		#endregion

		#region Private Methods
		private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is PasswordBox passwordBox)
			{
				if ((bool)e.OldValue)
				{
					passwordBox.PasswordChanged -= PasswordBox_Password_Changed;
				}

				if ((bool)e.NewValue)
				{
					passwordBox.PasswordChanged += PasswordBox_Password_Changed;
				}
			}
		}

		private static bool GetIsUpdating(DependencyObject dp) => (bool)dp.GetValue(IsUpdatingProperty);

		private static void Password_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			PasswordBox passwordBox = (PasswordBox)sender;
			passwordBox.PasswordChanged -= PasswordBox_Password_Changed;

			if (!GetIsUpdating(passwordBox))
			{
				passwordBox.Password = (string)e.NewValue;
			}

			passwordBox.PasswordChanged += PasswordBox_Password_Changed;
		}

		private static void PasswordBox_Password_Changed(object sender, RoutedEventArgs e)
		{
			PasswordBox passwordBox = (PasswordBox)sender;
			SetIsUpdating(passwordBox, true);
			SetPassword(passwordBox, passwordBox.Password);
			SetIsUpdating(passwordBox, false);
		}

		private static void SetIsUpdating(DependencyObject dp, bool value) => dp.SetValue(IsUpdatingProperty, value);
		#endregion
	}
}

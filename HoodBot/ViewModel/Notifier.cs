﻿namespace RobinHood70.HoodBot.ViewModel
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;

	public abstract class Notifier : INotifyPropertyChanged
	{
		#region Public Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Protected Virtual Methods
		protected virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		#endregion

		#region Protected Methods
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#", Justification = "No way around this.")]
		protected bool Set<T>(ref T field, T value, string propertyName)
		{
			var retval = !EqualityComparer<T>.Default.Equals(field, value);
			if (retval)
			{
				field = value;
				this.OnPropertyChanged(propertyName);
			}

			return retval;
		}
		#endregion
	}
}

namespace RobinHood70.HoodBot.ViewModel
{
	using System.Collections.Generic;
	using System.ComponentModel;

	public abstract class Notifier : INotifyPropertyChanged
	{
		#region Public Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Protected Virtual Methods
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => this.PropertyChanged?.Invoke(this, e);
		#endregion

		#region Protected Methods
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "0#", Justification = "No way around this.")]
		private protected bool Set<T>(ref T field, T value, string propertyName)
		{
			var retval = !EqualityComparer<T>.Default.Equals(field, value);
			if (retval)
			{
				field = value;
				this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
			}

			return retval;
		}
		#endregion
	}
}

namespace RobinHood70.Robby
{
	using System;

	public class WarningEventArgs : EventArgs
	{
		#region Constructors
		public WarningEventArgs(IMessageSource sender, string warning)
		{
			this.Sender = sender;
			this.Warning = warning;
		}
		#endregion

		#region Public Properties
		public IMessageSource Sender { get; }

		public string Warning { get; }
		#endregion
	}
}

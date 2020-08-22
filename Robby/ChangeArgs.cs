namespace RobinHood70.Robby
{
	using System.Collections.Generic;

	/// <summary>Event data for any change to the wiki, excluding page text changes, which are their own separate event.</summary>
	public class ChangeArgs
	{
		#region Fields
		private bool cancelChange;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ChangeArgs"/> class.</summary>
		/// <param name="realSender">The real sending object.</param>
		/// <param name="methodName">The method name.</param>
		/// <param name="parameters">Any parameters to the method. If null, Parameters will be set to an empty dictionary.</param>
		public ChangeArgs(object realSender, string methodName, IReadOnlyDictionary<string, object?>? parameters)
		{
			this.RealSender = realSender;
			this.MethodName = methodName;
			this.Parameters = parameters ?? new Dictionary<string, object?>();
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether the desired edit should be cancelled.</summary>
		/// <value><see langword="true"/> if the change should be cancelled; otherwise, <see langword="false"/>.</value>
		/// <remarks>Once this property is set to <see langword="true"/>, it cannot be changed.</remarks>
		public bool CancelChange
		{
			get => this.cancelChange;
			set => this.cancelChange |= value;
		}

		/// <summary>Gets the sender of the warning.</summary>
		/// <value>The sender of the warning.</value>
		public string MethodName { get; }

		/// <summary>Gets the parameters of the method that published the event.</summary>
		/// <value>The parameters.</value>
		public IReadOnlyDictionary<string, object?> Parameters { get; }

		/// <summary>Gets the actual sending object, since the sender in the event method will always appear to be the Site object.</summary>
		/// <value>The sender.</value>
		public object RealSender { get; }
		#endregion
	}
}

namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon.RequestBuilder;

	/// <summary>EventArgs used for the GettingResult event.</summary>
	/// <remarks>Initializes a new instance of the <see cref="RequestEventArgs" /> class.</remarks>
	/// <param name="request">The request object being submitted.</param>
	public class RequestEventArgs(Request request) : EventArgs
	{
		#region Public Properties

		/// <summary>Gets the request object being submitted.</summary>
		/// <value>The request.</value>
		public Request Request { get; } = request;
		#endregion
	}
}
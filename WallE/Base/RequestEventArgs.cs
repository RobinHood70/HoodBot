#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RequestBuilder;

	/// <summary>EventArgs used for the GettingResult event.</summary>
	public class RequestEventArgs : EventArgs
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="RequestEventArgs" /> class.</summary>
		/// <param name="request">The request object being submitted.</param>
		public RequestEventArgs(Request request) => this.Request = request;
		#endregion

		#region Public Properties

		/// <summary>Gets the request object being submitted.</summary>
		/// <value>The request.</value>
		public Request Request { get; }
		#endregion
	}
}

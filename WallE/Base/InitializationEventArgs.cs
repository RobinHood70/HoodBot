namespace RobinHood70.WallE.Base
{
	using System;

	/// <summary>Stores the inputs and the responses for any requests made to the wiki during the initialization routine. This potentially allows requests to be combined between layers.</summary>
	/// <seealso cref="System.EventArgs" />
	public class InitializationEventArgs : EventArgs
	{
		/// <summary>Initializes a new instance of the <see cref="InitializationEventArgs"/> class.</summary>
		/// <param name="input">The SiteInfo input.</param>
		/// <param name="result">The SiteInfo result.</param>
		public InitializationEventArgs(SiteInfoInput input, SiteInfoResult result)
		{
			this.Input = input;
			this.Result = result;
		}

		/// <summary>Gets the SiteInfo input.</summary>
		/// <value>The SiteInfo input.</value>
		public SiteInfoInput Input { get; }

		/// <summary>Gets the SiteInfo result.</summary>
		/// <value>The SiteInfo result.</value>
		public SiteInfoResult Result { get; }
	}
}
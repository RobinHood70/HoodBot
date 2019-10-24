namespace RobinHood70.WallE.Base
{
	using System;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Stores the inputs and the responses for any requests made to the wiki during the initialization routine. This potentially allows requests to be combined between layers.</summary>
	/// <seealso cref="EventArgs" />
	public class InitializedEventArgs : EventArgs
	{
		/// <summary>Initializes a new instance of the <see cref="InitializedEventArgs"/> class.</summary>
		/// <param name="input">The SiteInfo input.</param>
		/// <param name="result">The SiteInfo result. Will be null if initialization is not yet complete.</param>
		public InitializedEventArgs(SiteInfoInput input, SiteInfoResult result)
		{
			ThrowNull(input, nameof(input));
			this.Input = input;
			this.Result = result;
		}

		/// <summary>Gets the final input parameters, should they be needed.</summary>
		/// <value>The final input parameters.</value>
		public SiteInfoInput Input { get; }

		/// <summary>Gets the SiteInfo result.</summary>
		/// <value>The SiteInfo result.</value>
		public SiteInfoResult Result { get; }
	}
}
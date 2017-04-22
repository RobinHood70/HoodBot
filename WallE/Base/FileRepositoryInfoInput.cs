#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class FileRepositoryInfoInput
	{
		#region Public Properties

		/// <summary>Gets or sets a string-based list of properties to retrieve.</summary>
		/// <value>The properties.</value>
		/// <remarks>Unlike the Properties value on other inputs, this one is left as a collection of strings due to the flexible nature of the module.</remarks>
		public IEnumerable<string> Properties { get; set; }
		#endregion
	}
}

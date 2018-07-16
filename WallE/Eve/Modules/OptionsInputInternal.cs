#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;

	internal class OptionsInputInternal
	{
		#region Public Properties
		public IEnumerable<string> Change { get; set; }

		public string OptionName { get; set; }

		public string OptionValue { get; set; }

		public bool Reset { get; set; }

		public IEnumerable<string> ResetKinds { get; set; }

		public string Token { get; set; }
		#endregion
	}
}
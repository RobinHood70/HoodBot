namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;

	internal sealed class OptionsInputInternal
	{
		#region Constructors
		public OptionsInputInternal(string token, string name, string? value)
			: this(token, [])
		{
			this.OptionName = name;
			this.OptionValue = value;
		}

		public OptionsInputInternal(string token, IEnumerable<string> change)
		{
			this.Change = change;
			this.Token = token;
		}
		#endregion

		#region Public Properties
		public IEnumerable<string> Change { get; set; }

		public string? OptionName { get; set; }

		public string? OptionValue { get; set; }

		public bool Reset { get; set; }

		public IEnumerable<string>? ResetKinds { get; set; }

		public string Token { get; set; }
		#endregion
	}
}
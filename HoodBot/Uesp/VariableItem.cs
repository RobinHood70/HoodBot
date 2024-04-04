namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;

	public class VariableItem
	{
		#region Constructors
		public VariableItem(IReadOnlyDictionary<string, string> dictionary, string? set)
		{
			ArgumentNullException.ThrowIfNull(dictionary);
			this.Dictionary = dictionary;
			this.Set = set;
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, string> Dictionary { get; }

		public string? Set { get; }
		#endregion
	}
}
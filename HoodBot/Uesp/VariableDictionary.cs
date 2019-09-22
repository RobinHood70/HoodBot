namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	/// <summary>Class VariableDictionary. Currently, its only purpose is to avoid nested generic types. Implements the <see cref="ReadOnlyDictionary{string, string}" />.</summary>
	/// <seealso cref="ReadOnlyDictionary{string, string}" />
	public class VariableDictionary : ReadOnlyDictionary<string, string>
	{
		public VariableDictionary(IDictionary<string, string> dictionary)
			: base(dictionary)
		{
		}
	}
}

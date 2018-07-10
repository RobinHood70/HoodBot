namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	public sealed class ConstructorParameter : IEquatable<ConstructorParameter>
	{
		#region Constructors
		public ConstructorParameter(string label, ParameterInfo info)
		{
			this.Label = label ?? UnCamelCase(info.Name);
			this.Name = info.Name;
			this.Type = info.ParameterType;
		}
		#endregion

		#region Public Properties
		public string Label { get; }

		public string Name { get; }

		public Type Type { get; }

		public object Value { get; set; }
		#endregion

		#region Public Methods
		public bool Equals(ConstructorParameter other)
		{
			if (other == null)
			{
				return false;
			}

			return this.Label == other.Label && this.Name == other.Name && this.Type == other.Type && this.Value == other.Value;
		}
		#endregion

		#region Private Static Methods
		private static string UnCamelCase(string name)
		{
			name = char.ToUpperInvariant(name[0]) + (name.Length > 1 ? name.Substring(1) : string.Empty);
			var words = new List<string>(5);
			var word = string.Empty;
			var lastWasCapital = false;
			var didWordBreak = false;
			foreach (var c in name)
			{
				if (char.IsUpper(c) && !lastWasCapital)
				{
					words.Add(word);
					word = c.ToString();
					lastWasCapital = true;
					didWordBreak = true;
				}
				else if (!char.IsUpper(c) && lastWasCapital && !didWordBreak)
				{
					words.Add(word.Substring(0, word.Length - 1));
					word = word.Substring(word.Length - 1) + c;
					didWordBreak = true;
				}
				else
				{
					word += c;
					lastWasCapital = char.IsUpper(c);
					didWordBreak = false;
				}
			}

			words.Add(word);
			return string.Join(" ", words);
		}
		#endregion
	}
}
namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Reflection;
	using RobinHood70.HoodBot.Jobs.Design;
	using static RobinHood70.WikiCommon.Globals;

	public sealed class ConstructorParameter : IEquatable<ConstructorParameter>
	{
		#region Constructors
		public ConstructorParameter(ParameterInfo parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			var attributes = parameter.GetCustomAttributes(typeof(JobParameterAttribute), true);
			if (attributes.Length > 1)
			{
				throw new InvalidOperationException($"Multiple JobParameterAttribute derivatives specified on parameter \"{parameter.Name}\" in constructor: {FormatMember(parameter)}");
			}

			this.Attribute = attributes.Length == 1 ? attributes[0] as JobParameterAttribute : null;
			this.Label = this.Attribute?.Label ?? UnCamelCase(parameter.Name);
			this.Name = parameter.Name;
			this.Type = parameter.ParameterType;
			if (this.Attribute?.DefaultValue != null)
			{
				this.Value = this.Attribute.DefaultValue;
			}
			else if (parameter.ParameterType.IsValueType)
			{
				this.Value = Activator.CreateInstance(parameter.ParameterType);
			}
		}
		#endregion

		#region Public Properties
		public JobParameterAttribute Attribute { get; }

		public string Label { get; }

		public string Name { get; }

		public Type Type { get; }

		public object Value { get; set; }
		#endregion

		#region Public Methods
		public bool Equals(ConstructorParameter other) => other == null
			? false
			: this.Label == other.Label && this.Name == other.Name && this.Type == other.Type && this.Value == other.Value;
		#endregion

		#region Public Override Methods
		public override bool Equals(object obj) => this.Equals(obj as ConstructorParameter);

		public override int GetHashCode() => CompositeHashCode(this.Label, this.Name, this.Type, this.Value);

		public override string ToString() => this.Type.Name + ' ' + this.Name;
		#endregion

		#region Private Static Methods
		private static string FormatMember(ParameterInfo parameter) => parameter.Member.ToString()
			.Substring(5)
			.Replace("System.", string.Empty)
			.Replace("Collections.Generic.", string.Empty)
			.Replace("Collections.ObjectModel.", string.Empty)
			.Replace("RobinHood70.Robby.", string.Empty)
			.Replace("RobinHood70.HoodBot.Jobs.Design.", string.Empty);

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
					word = c.ToString(CultureInfo.InvariantCulture);
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
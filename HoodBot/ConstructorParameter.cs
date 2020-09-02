namespace RobinHood70.HoodBot
{
	using System;
	using System.Reflection;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using static RobinHood70.CommonCode.Globals;

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
			this.Name = parameter.Name ?? throw PropertyNull(nameof(parameter), nameof(parameter.Name));
			this.Type = parameter.ParameterType;
			this.Label = this.Attribute?.Label ?? this.Name.UnCamelCase();
			this.Value =
				this.Attribute?.DefaultValue != null ? this.Attribute.DefaultValue :
				this.Type.IsValueType ? Activator.CreateInstance(this.Type) :
				null;
		}
		#endregion

		#region Public Properties
		public JobParameterAttribute? Attribute { get; }

		public string Label { get; }

		public string Name { get; }

		public Type Type { get; }

		public object? Value { get; set; }
		#endregion

		#region Public Methods
		public bool Equals(ConstructorParameter? other) =>
			other != null &&
			this.Label == other.Label &&
			this.Name == other.Name &&
			this.Type == other.Type &&
			this.Value == other.Value;
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => this.Equals(obj as ConstructorParameter);

		public override int GetHashCode() => CompositeHashCode(this.Label, this.Name, this.Type, this.Value);

		public override string ToString() => this.Type.Name + ' ' + this.Name;
		#endregion

		#region Private Static Methods
		private static string FormatMember(ParameterInfo parameter)
		{
			var memberName = parameter.Member.ToString() ?? throw new InvalidOperationException();
			return memberName
					 .Substring(5)
					 .Replace("System.", string.Empty, StringComparison.Ordinal)
					 .Replace("Collections.Generic.", string.Empty, StringComparison.Ordinal)
					 .Replace("Collections.ObjectModel.", string.Empty, StringComparison.Ordinal)
					 .Replace("RobinHood70.Robby.", string.Empty, StringComparison.Ordinal)
					 .Replace("RobinHood70.HoodBot.Jobs.Design.", string.Empty, StringComparison.Ordinal);
		}
		#endregion
	}
}
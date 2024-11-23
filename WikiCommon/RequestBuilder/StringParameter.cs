namespace RobinHood70.WikiCommon.RequestBuilder;

using System;

#region Public Enumerations

/// <summary>The type of value stored in the string. Note that implementers are not forced to respect these values.</summary>
public enum ValueType
{
	/// <summary>The value remains the same, regardless of whether it's displayed or sent to the server.</summary>
	Normal = 0,

	/// <summary>The value should be hidden when displayed.</summary>
	Hidden,

	/// <summary>The value should be modified when displayed (e.g., <c>format=json</c> should be displayed as <c>format=jsonfm</c>).</summary>
	Modify,
}
#endregion

/// <summary>Represents a string parameter.</summary>
/// <seealso cref="Parameter" />
/// <remarks>Initializes a new instance of the <see cref="StringParameter" /> class.</remarks>
/// <param name="name">The parameter name.</param>
/// <param name="value">The parameter value.</param>
/// <param name="type">The type of data stored in the value.</param>
/// <remarks><see langword="null"/> is a valid value for this parameter type, so no input validation is performed.</remarks>
public class StringParameter(string name, string? value, ValueType type) : Parameter(name)
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="StringParameter" /> class.</summary>
	/// <param name="name">The parameter name.</param>
	/// <param name="value">The parameter value.</param>
	/// <remarks><see langword="null"/> is a valid value for this parameter type, so no input validation is performed.</remarks>
	public StringParameter(string name, string? value)
		: this(name, value, ValueType.Normal)
	{
	}
	#endregion

	#region Public Properties

	/// <summary>Gets the value of the parameter.</summary>
	public string Value { get; } = value ?? string.Empty;

	/// <summary>Gets the type of string stored in <see cref="Value"/>.</summary>
	public ValueType ValueType { get; } = type;
	#endregion

	#region Public Override Methods

	/// <summary>Accepts the specified visitor.</summary>
	/// <param name="visitor">The visitor.</param>
	/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
	public override void Accept(IParameterVisitor visitor)
	{
		ArgumentNullException.ThrowIfNull(visitor);
		visitor.Visit(this);
	}

	/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
	/// <returns>A <see cref="string" /> that represents this instance.</returns>
	public override string ToString() => this.Name + "=" + this.Value;
	#endregion
}
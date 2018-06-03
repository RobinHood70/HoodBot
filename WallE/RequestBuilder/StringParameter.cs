﻿namespace RobinHood70.WallE.RequestBuilder
{
	/// <summary>Represents a string parameter.</summary>
	/// <seealso cref="Parameter{T}" />
	public class StringParameter : Parameter<string>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="StringParameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		public StringParameter(string name, string value)
			: base(name, value)
		{
		}
		#endregion

		#region Public Override Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <a href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</a> article if you are not familiar with this pattern.</remarks>
		public override void Accept(IParameterVisitor visitor) => visitor?.Visit(this);

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name + "=" + this.Value;
		#endregion
	}
}

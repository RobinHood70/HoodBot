namespace RobinHood70.WallE.RequestBuilder
{
	using System.Collections.Generic;

	/// <summary>Represents a parameter with collection of unique values, normally separated by pipe characters.</summary>
	/// <seealso cref="Parameter{T}" />
	public class PipedParameter : Parameter<HashSet<string>>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PipedParameter"/> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values. Any duplicates in the input will be ignored.</param>
		public PipedParameter(string name, IEnumerable<string> values)
			: base(name, new HashSet<string>(values))
		{
		}
		#endregion

		#region Public Override Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		public override void Accept(IParameterVisitor visitor) => visitor?.Visit(this);

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name + "=" + string.Join("|", this.Value);
		#endregion
	}
}

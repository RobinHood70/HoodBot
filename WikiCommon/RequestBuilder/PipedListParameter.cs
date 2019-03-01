namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a parameter with collection of values, normally separated by pipe characters. All values added to the parameter will be emitted, regardless of any duplication.</summary>
	/// <seealso cref="Parameter{T}" />
	public class PipedListParameter : Parameter<List<string>>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PipedListParameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		public PipedListParameter(string name, IEnumerable<string> values)
				: base(name)
		{
			ThrowNull(values, nameof(values));
			this.Value = new List<string>(values);
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
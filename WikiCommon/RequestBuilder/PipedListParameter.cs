namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Represents a parameter with collection of values, normally separated by pipe characters. All values added to the parameter will be emitted, regardless of any duplication.</summary>
	/// <seealso cref="Parameter" />
	public class PipedListParameter : MultiValuedParameter
	{
		#region Fields
		private readonly string[] values;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PipedListParameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		public PipedListParameter(string name, IEnumerable<string> values)
			: base(name ?? throw ArgumentNull(nameof(name))) => this.values = new List<string>(values ?? Array.Empty<string>()).ToArray();
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public override IEnumerable<string> Values => this.values;
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override void Accept(IParameterVisitor visitor)
		{
			ThrowNull(visitor, nameof(visitor));
			visitor.Visit(this);
		}

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name + "=" + string.Join("|", this.Values);
		#endregion
	}
}
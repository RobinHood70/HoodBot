namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System.Collections.Generic;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Represents a parameter with collection of unique values, normally separated by pipe characters.</summary>
	/// <seealso cref="Parameter" />
	public class PipedParameter : MultiValuedParameter
	{
		#region Fields
		private readonly HashSet<string> values;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PipedParameter" /> class.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values. Any duplicates in the input will be ignored.</param>
		public PipedParameter(string name, IEnumerable<string> values)
			: base(name) => this.values = new HashSet<string>(values ?? throw ArgumentNull(nameof(values)));
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public override IEnumerable<string> Values => this.values;
		#endregion

		#region Public Methods

		/// <summary>Adds a collection of items to the Values collection.</summary>
		/// <param name="values">The values to add.</param>
		public void Add(IEnumerable<string> values) => this.values.UnionWith(values ?? throw ArgumentNull(nameof(values)));
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override void Accept(IParameterVisitor visitor)
		{
			ThrowNull(visitor, nameof(visitor));
			visitor.Visit(this);
		}

		/// <inheritdoc/>
		public override string ToString() => this.Name + "=" + string.Join("|", this.Values);
		#endregion
	}
}

namespace RobinHood70.WikiCommon.RequestBuilder
{
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a parameter with file information.</summary>
	/// <seealso cref="Parameter{T}" />
	public class FileParameter : Parameter<(string fileName, byte[] data)>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FileParameter" /> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="fileData">The file data.</param>
		public FileParameter(string name, string fileName, byte[] fileData)
			: base(name, (fileName, fileData))
		{
			ThrowNull(fileName, nameof(fileName));
			ThrowNull(fileData, nameof(fileData));
		}
		#endregion

		#region Public Override Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
		public override void Accept(IParameterVisitor visitor)
		{
			ThrowNull(visitor, nameof(visitor));
			visitor.Visit(this);
		}

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name + "=<filedata>";
		#endregion
	}
}

namespace RobinHood70.WikiCommon.RequestBuilder
{
	using RobinHood70.CommonCode;

	/// <summary>Represents a parameter with file information.</summary>
	/// <seealso cref="Parameter" />
	/// <remarks>Initializes a new instance of the <see cref="FileParameter" /> class.</remarks>
	/// <param name="name">The parameter name.</param>
	/// <param name="fileName">The name of the file.</param>
	/// <param name="fileData">The file data.</param>
	public class FileParameter(string name, string fileName, byte[] fileData) : Parameter(name)
	{
		#region Fields
		private readonly byte[] fileData = fileData.NotNull();
		#endregion

		#region Public Properties

		/// <summary>Gets the file name.</summary>
		public string FileName { get; } = fileName.NotNull();
		#endregion

		#region Public Methods

		/// <summary>Gets the file data.</summary>
		/// <returns>A byte array containing the file data.</returns>
		public byte[] GetData() => this.fileData; // Not a property due to CA1819.
		#endregion

		#region Public Override Methods

		/// <summary>Accepts the specified visitor.</summary>
		/// <param name="visitor">The visitor.</param>
		/// <remarks>See Wikipedia's <see href="https://en.wikipedia.org/wiki/Visitor_pattern">Visitor pattern</see> article if you are not familiar with this pattern.</remarks>
		public override void Accept(IParameterVisitor visitor) => visitor.NotNull().Visit(this);

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name + "=<filedata>";
		#endregion
	}
}
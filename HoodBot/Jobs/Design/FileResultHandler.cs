namespace RobinHood70.HoodBot.Jobs.Design
{
	using System.IO;
	using RobinHood70.CommonCode;

	/// <summary>Implements the <see cref="ResultHandler" /> class and stores results in a local file.</summary>
	/// <seealso cref="ResultHandler" />
	public class FileResultHandler : ResultHandler
	{
		#region Fields
		private readonly string fileName;
		#endregion

		/// <summary>Initializes a new instance of the <see cref="FileResultHandler"/> class.</summary>
		/// <param name="fileName">The file name to save to. Will be overwritten if it exists.</param>
		public FileResultHandler(string fileName)
			: base(null)
		{
			this.fileName = fileName.NotNull();
			_ = new FileInfo(fileName); // This will throw if the fileName is invalid.
		}

		/// <inheritdoc/>
		public override void Save() => File.WriteAllText(this.fileName, this.StringBuilder.ToString()); // No length checking on this one, since it's at least conceivable that a zero-length file might be desirable.
	}
}

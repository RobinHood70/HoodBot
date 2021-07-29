namespace RobinHood70.WallE.Base
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
	using RobinHood70.CommonCode;

	public class FileRevertInput
	{
		#region Constructors
		public FileRevertInput(string fileName, string archiveName)
		{
			this.FileName = fileName.NotNullOrWhiteSpace(nameof(fileName));
			this.ArchiveName = archiveName.NotNullOrWhiteSpace(nameof(archiveName));
		}
		#endregion

		#region Public Properties
		public string ArchiveName { get; }

		public string? Comment { get; set; }

		public string FileName { get; }

		public string? Token { get; set; }
		#endregion
	}
}

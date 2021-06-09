#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using static RobinHood70.CommonCode.Globals;

	public class FileRevertInput
	{
		#region Constructors
		public FileRevertInput(string fileName, string archiveName)
		{
			ThrowNullOrWhiteSpace(fileName, nameof(fileName));
			ThrowNullOrWhiteSpace(archiveName, nameof(archiveName));
			this.FileName = fileName;
			this.ArchiveName = archiveName;
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

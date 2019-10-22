#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// MWVERSION: 1.27
	internal class ActionFileRevert : ActionModule<FileRevertInput, FileRevertResult>
	{
		#region Constructors
		public ActionFileRevert(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 118;

		public override string Name { get; } = "filerevert";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FileRevertInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("filename", input.FileName)
				.AddIfNotNull("archivename", input.ArchiveName)
				.AddIfNotNull("comment", input.Comment)
				.AddHidden("token", input.Token);
		}

		protected override FileRevertResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			return new FileRevertResult(result.MustHaveString("result"));
		}
		#endregion
	}
}

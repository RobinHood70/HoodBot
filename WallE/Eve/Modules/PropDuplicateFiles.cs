#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropDuplicateFiles : PropListModule<DuplicateFilesInput, DuplicateFileItem>, IGeneratorModule
	{
		#region Constructors
		public PropDuplicateFiles(WikiAbstractionLayer wal, DuplicateFilesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 114;

		public override string Name { get; } = "duplicatefiles";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "df";
		#endregion

		#region Public Static Methods
		public static PropDuplicateFiles CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropDuplicateFiles(wal, input as DuplicateFilesInput);

		public static PropDuplicateFiles CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropDuplicateFiles(wal, input as DuplicateFilesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, DuplicateFilesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("localonly", input.LocalOnly)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override DuplicateFileItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new DuplicateFileItem()
			{
				Name = (string)result["name"],
				Shared = result["shared"].AsBCBool(),
				Timestamp = result["timestamp"].AsDate(),
				User = (string)result["user"],
			};
			return item;
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.DuplicateFiles);

		protected override void SetResultsOnCurrentPage() => this.Output.DuplicateFiles = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}

namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropDuplicateFiles : PropListModule<DuplicateFilesInput, DuplicateFileItem>, IGeneratorModule
	{
		#region Constructors
		public PropDuplicateFiles(WikiAbstractionLayer wal, DuplicateFilesInput input)
			: this(wal, input, null)
		{
		}

		public PropDuplicateFiles(WikiAbstractionLayer wal, DuplicateFilesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 114;

		public override string Name => "duplicatefiles";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "df";
		#endregion

		#region Public Static Methods
		public static PropDuplicateFiles CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (DuplicateFilesInput)input, pageSetGenerator);

		public static PropDuplicateFiles CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (DuplicateFilesInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, DuplicateFilesInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.Add("localonly", input.LocalOnly)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override DuplicateFileItem? GetItem(JToken result) => result == null
			? null
			: new DuplicateFileItem(
				name: result.MustHaveString("name"),
				shared: result["shared"].GetBCBool(),
				timestamp: result.MustHaveDate("timestamp"),
				user: result.MustHaveString("user"));

		protected override IList<DuplicateFileItem> GetMutableList(PageItem page) => page.DuplicateFiles;
		#endregion
	}
}
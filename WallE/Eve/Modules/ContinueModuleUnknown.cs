namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class ContinueModuleUnknown : ContinueModule
	{
		#region Public Override Properties
		public override int MinimumVersion => 109;

		public override string Name => string.Empty;
		#endregion

		#region Public Override Methods
		public override void BuildRequest(Request request) => request
				.NotNull()
				.Add(ContinueModule2.ContinueName);

		public override ContinueModule Deserialize(WikiAbstractionLayer wal, JToken parent)
		{
			if (parent == null)
			{
				return this;
			}

			var newVersion =
				parent[ContinueModule2.ContinueName] != null ? 2 :
				parent[ContinueModule1.ContinueName] != null ? 1 :
				0;

			if (newVersion == 0)
			{
				return this;
			}

			wal.ContinueVersion = newVersion;
			var newModule = wal.ModuleFactory.CreateContinue();
			return newModule.Deserialize(wal, parent);
		}
		#endregion
	}
}
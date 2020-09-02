#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;

	internal class ContinueModuleUnknown : ContinueModule
	{
		#region Public Override Properties
		public override int MinimumVersion => 109;

		public override string Name => string.Empty;
		#endregion

		#region Public Override Methods
		public override void BuildRequest(Request request)
		{
			ThrowNull(request, nameof(request));
			request.Add(ContinueModule2.ContinueName);
		}

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

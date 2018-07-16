#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ContinueModuleUnknown : ContinueModule
	{
		#region Public Override Methods
		public override void BuildRequest(Request request)
		{
			ThrowNull(request, nameof(request));
			request.Add(ContinueModule2.Name);
		}

		public override int Deserialize(JToken parent) =>
			parent == null ? 0 :
			parent[ContinueModule2.Name] != null ? 2 :
			parent[ContinueModule1.Name] != null ? 1 : 0;
		#endregion
	}
}

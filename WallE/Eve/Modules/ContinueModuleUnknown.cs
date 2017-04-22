#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ContinueModuleUnknown : ContinueModuleBase
	{
		#region Protected Override Methods
		public override void BuildRequest(Request request)
		{
			ThrowNull(request, nameof(request));
			request.Add(ContinueModule2.Name);
		}

		public override int Deserialize(JToken parent)
		{
			if (parent != null)
			{
				if (parent[ContinueModule2.Name] != null)
				{
					return 2;
				}
				else if (parent[ContinueModule1.Name] != null)
				{
					return 1;
				}
			}

			return 0;
		}
		#endregion
	}
}

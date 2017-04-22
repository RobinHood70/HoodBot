namespace WallE.Implementations.Api
{
	using System;
	using Http;
	using Newtonsoft.Json.Linq;

	public interface IApiModule
	{
		#region Properties
		Version MinimumVersion { get; }

		string Name { get; }

		string Prefix { get; }

		string ResultName { get; }
		#endregion

		#region Methods
		void BuildRequest(PhpRequest request);

		void Deserialize(JToken parent);
		#endregion
	}
}

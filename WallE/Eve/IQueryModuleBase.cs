using System;
using Newtonsoft.Json.Linq;
using WallE.Http;

namespace WallE.Implementations.Api
{
	public interface IQueryModuleBase<TInput, TOutput>
	{
		string BasePrefix {
			get;
		}
		string ContinueName {
			get;
		}
		bool ContinueParsing {
			get;
		}
		TInput Input {
			get;
		}
		bool IsGenerator {
			get;
		}
		Version MinimumVersion {
			get;
		}
		string Name {
			get;
		}
		TOutput Output {
			get;
		}
		string Prefix {
			get;
		}
		ActionQuery Query {
			get;
		}
		string ResultName {
			get;
		}
		string Type {
			get;
		}

		void BuildRequest(PhpRequest request);
		void Deserialize(JToken parent);
		void OnSubmit(ActionQuery query);
		bool SwallowWarnings(string code, string line);
	}
}
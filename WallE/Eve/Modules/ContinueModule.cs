#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.RequestBuilder;

	public abstract class ContinueModule
	{
		#region Constructors
		protected ContinueModule()
		{
		}
		#endregion

		#region Public Properties
		public bool BatchComplete { get; protected set; }

		public bool Continues { get; protected set; }
		#endregion

		#region Protected Internal Properties
		protected internal string GeneratorContinue { get; set; } = string.Empty;
		#endregion

		#region Protected Properties
		protected Dictionary<string, string> ContinueEntries { get; } = new Dictionary<string, string>();
		#endregion

		#region Public Abstract Methods
		public abstract void BuildRequest(Request request);

		// Since this isn't a traditional module, we're slightly perverting Deserialize here. It should return a continue version number if the continue version was previously unknown and is now known; otherwise, it should return 0.
		public abstract int Deserialize(JToken parent);
		#endregion

		#region Public Virtual Methods
		public virtual void OnSubmit(IPageSetInternal pageSet)
		{
			var generator = pageSet?.Generator;
			this.GeneratorContinue = generator?.FullPrefix + generator?.ContinueName;
		}
		#endregion
	}
}

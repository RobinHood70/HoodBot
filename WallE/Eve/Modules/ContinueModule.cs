#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

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

		#region Protected Properties

		// Null values are almost certainly an error, but MediaWiki knows best, so trust that if it gives us one, we should use it.
		protected Dictionary<string, string?> ContinueEntries { get; } = new Dictionary<string, string?>();

		protected string? GeneratorContinue { get; private set; }
		#endregion

		#region Public Abstract Methods
		public abstract void BuildRequest(Request request);

		// Since this isn't a traditional module, we're slightly perverting Deserialize here. It should return a continue version number if the continue version was previously unknown and is now known; otherwise, it should return 0.
		public abstract ContinueModule Deserialize(WikiAbstractionLayer wal, JToken parent);
		#endregion

		#region Public Virtual Methods
		public virtual void BeforePageSetSubmit(IPageSetGenerator pageSet)
		{
			ThrowNull(pageSet, nameof(pageSet));
			this.GeneratorContinue = pageSet.Generator is IGeneratorModule generator
				? generator.FullPrefix + generator.ContinueName
				: null;
		}
		#endregion
	}
}

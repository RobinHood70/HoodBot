#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class ContinueModule2 : ContinueModule
	{
		#region Public Constants
		public const int MinimumVersion = 121;
		public const string Name = "continue";
		#endregion

		#region Private Constants
		private const int BatchVersion = 126;
		#endregion

		#region Fields
		private readonly bool addContinue;
		private readonly bool supportsBatch;
		#endregion

		#region Constructors
		public ContinueModule2(int siteVersion100)
		{
			// Version 1.26 emits a warning when not emitting continue=, and lower versions require it, so we track it separately from supportsBatch.
			this.addContinue = siteVersion100 <= BatchVersion;
			this.supportsBatch = siteVersion100 >= BatchVersion;
		}
		#endregion

		#region Protected Override Methods
		public override void BuildRequest(Request request)
		{
			ThrowNull(request, nameof(request));
			if (this.Continues)
			{
				foreach (var entry in this.ContinueEntries)
				{
					request.AddOrChangeIfNotNull(entry.Key, entry.Value);
				}

				this.Continues = false;
			}
			else if (this.addContinue)
			{
				request.Add(Name);
			}
		}

		public override ContinueModule Deserialize(WikiAbstractionLayer wal, JToken parent)
		{
			if (parent == null)
			{
				return this;
			}

			this.BatchComplete = this.supportsBatch ? parent["batchcomplete"].GetBCBool() : true;
			if (parent[Name] is JToken result && result.Type != JTokenType.Null)
			{
				this.Continues = true;
				this.ContinueEntries.Clear();
				foreach (var node in result.Children<JProperty>())
				{
					this.ContinueEntries.Add(node.Name, (string?)node.Value ?? throw ParsingExtensions.MalformedTypeException(nameof(System.String), node));
				}

				// Figure out whether or not the batch is complete manually. We don't need to worry about the case of no continue entries here, since we should never be executing this code in that event.
				if (!this.supportsBatch)
				{
					// For pre-MW 1.26, figure out whether or not the batch is complete manually. We don't need to worry about the case of no continue entries here, since we should never be executing this code in that event.
					this.BatchComplete = this.ContinueEntries.Count <= 2 && (this.GeneratorContinue == null ? true : this.ContinueEntries.ContainsKey(this.GeneratorContinue));
				}
			}

			return this;
		}
		#endregion
	}
}
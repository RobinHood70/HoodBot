#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Diagnostics;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Properties.EveMessages;
	using static RobinHood70.WikiCommon.Globals;

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
		private readonly bool supportsBatch;
		#endregion

		#region Constructors
		public ContinueModule2(int siteVersion100) => this.supportsBatch = siteVersion100 >= BatchVersion;
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
			else if (!this.supportsBatch)
			{
				request.Add(Name);
			}
		}

		public override int Deserialize(JToken parent)
		{
			if (parent == null)
			{
				return 0;
			}

			var result = parent[Name];
			if (result == null || result.Type == JTokenType.Null)
			{
				return 0;
			}

			this.BatchComplete = this.supportsBatch ? parent["batchcomplete"].AsBCBool() : true;
			this.Continues = true;
			this.ContinueEntries.Clear();
			result.AddPropertiesToDictionary(this.ContinueEntries);

			// Figure out whether or not the batch is complete manually. We don't need to worry about the case of no continue entries here, since we should never be executing this code in that event.
			if (!this.supportsBatch)
			{
				// For pre-MW 1.26, figure out whether or not the batch is complete manually. We don't need to worry about the case of no continue entries here, since we should never be executing this code in that event.
				this.BatchComplete = (this.ContinueEntries.Count <= 2) && this.ContinueEntries.ContainsKey(this.GeneratorContinue);
			}
#if DEBUG
			else if (this.BatchComplete != (this.ContinueEntries.Count <= 2) && this.ContinueEntries.ContainsKey(this.GeneratorContinue))
			{
				// This is a temporary check against the method we're using in the Else block for MW 1.21-1.25.
				Debug.WriteLine($"Calculated BatchComplete does not match returned batch complete:");
				Debug.Indent();
				Debug.WriteLine("Returned: " + this.BatchComplete);
				Debug.WriteLine("Continue Entries:");
				Debug.Indent();
				foreach (var entry in this.ContinueEntries)
				{
					Debug.WriteLine(CurrentCulture(ColonText, entry.Key, entry.Value));
				}

				Debug.Unindent();
				Debug.Unindent();
				Debug.Fail("Please fix!");
			}
#endif

			return 0;
		}
		#endregion
	}
}
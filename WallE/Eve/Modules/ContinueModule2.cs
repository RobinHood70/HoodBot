namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.Exceptions;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ContinueModule2(int siteVersion100) : ContinueModule
{
	#region Public Constants
	public const int ContinueMinimumVersion = 121;
	public const string ContinueName = "continue";
	#endregion

	#region Private Constants
	private const int BatchVersion = 126;
	#endregion

	#region Fields
	private readonly bool addContinue = siteVersion100 <= BatchVersion;
	private readonly bool supportsBatch = siteVersion100 >= BatchVersion;
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => ContinueMinimumVersion;

	public override string Name => ContinueName;
	#endregion

	#region Protected Override Methods
	public override void BuildRequest(Request request)
	{
		ArgumentNullException.ThrowIfNull(request);
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
			request.Add(this.Name);
		}
	}

	public override ContinueModule Deserialize(WikiAbstractionLayer wal, JToken parent)
	{
		if (parent == null)
		{
			return this;
		}

		this.BatchComplete = !this.supportsBatch || parent["batchcomplete"].GetBCBool();
		if (parent[this.Name] is JToken result && result.Type != JTokenType.Null)
		{
			this.Continues = true;
			this.ContinueEntries.Clear();
			foreach (var node in result.Children<JProperty>())
			{
				this.ContinueEntries.Add(node.Name, (string?)node.Value ?? throw MalformedTypeException(nameof(String), node));
			}

			// Figure out whether or not the batch is complete manually. We don't need to worry about the case of no continue entries here, since we should never be executing this code in that event.
			if (!this.supportsBatch)
			{
				// For pre-MW 1.26, figure out whether or not the batch is complete manually. We don't need to worry about the case of no continue entries here, since we should never be executing this code in that event.
				this.BatchComplete = this.ContinueEntries.Count <= 2 && (this.GeneratorContinue == null || this.ContinueEntries.ContainsKey(this.GeneratorContinue));
			}
		}

		return this;
	}
	#endregion
}
namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using NetTools;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Wikimedia;
using RobinHood70.Robby;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Clients;
using RobinHood70.WallE.Eve;
using RobinHood70.WikiCommon;

internal sealed class ImportBlocks : WikiJob
{
	#region Static Fields
	private static readonly HashSet<string> Sites = new(StringComparer.Ordinal)
	{
		"en.uesp.net",
		"starfieldwiki.net",
		//// "ar.uesp.net",
		//// "fr.uesp.net",
		//// "it.uesp.net",
		//// "pt.uesp.net",
		"ck.uesp.net",
		"cs.uesp.net",
		"falloutck.uesp.net"
	};
	#endregion

	#region Fields

	// Regex initialized from file to keep specific filters hidden from vandals.
	private readonly Regex blockFilter;
	private readonly IMediaWikiClient client;
	private readonly List<IPAddressRange> exceptions = [];

	[JobInfo("Import Blocks")]
	[NoLogin]
	#endregion

	#region Constructors
	public ImportBlocks(JobManager jobManager)
		: base(jobManager, JobType.Write)
	{
		this.client = jobManager.Client;
		var (regex, exceptionsText) = ParseBlocksFilter();
		this.blockFilter = regex;
		this.exceptions.AddRange(this.GetCloudflareExceptions());
	}
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		// This is a little bit ugly in crossing responsibility boundaries by accessing App directly. Could also have a second copy with Settings.Load. Debatable which is better/uglier.
		var wikis = App.UserSettings.Wikis;
		foreach (var wiki in wikis)
		{
			if (wiki.WikiInfo.Api is Uri uri && Sites.Contains(uri.Host) && wiki.UserName is not null)
			{
				var api = new WikiAbstractionLayer(this.client, wiki.Api!);
				api.SendingRequest += JobManager.WalSendingRequest;
				var site = this.JobManager.CreateSite(wiki.WikiInfo, api, this.Site.EditingEnabled);
				site.Login(wiki.UserName, wiki.Password);

				this.StatusWriteLine(string.Empty);
				this.StatusWriteLine("Getting blocks for " + site.Name);
				var localBlocks = this.GetLocalBlocks(site);
				var lastRun = GetStartTime(wiki.UserName, localBlocks);
				var wmfBlocks = this.GetWmfBlocks(lastRun);
				var blockUpdates = DoSiteBlocks(wmfBlocks, localBlocks);

				this.StatusWriteLine($"Applying {blockUpdates.UpdateCount} updated and {blockUpdates.NewCount} new blocks ({blockUpdates.OverlapCount} overlaps)");
				this.UpdateBlocks(site, blockUpdates.NewBlocks);
				api.SendingRequest -= JobManager.WalSendingRequest;
			}
		}
	}
	#endregion

	#region Private Static Methods
	private static BlockUpdates DoSiteBlocks(List<CommonBlock> wmfBlocks, Dictionary<string, Block> localBlocks)
	{
		var newBlocks = new SortedDictionary<string, CommonBlock>(StringComparer.Ordinal);
		var overlapCount = 0;
		var updateCount = 0;
		var newCount = 0;
		foreach (var wmfBlock in wmfBlocks)
		{
			if (wmfBlock.Address is not null)
			{
				if (newBlocks.TryGetValue(wmfBlock.Address, out var newBlock))
				{
					if (wmfBlock.Expiry > newBlock.Expiry)
					{
						overlapCount++;
						newBlock.Expiry = wmfBlock.Expiry;
						if (!string.IsNullOrWhiteSpace(wmfBlock.Source))
						{
							newBlock.Source = wmfBlock.Source;
						}
					}
					else if (string.IsNullOrWhiteSpace(newBlock.Source) && !string.IsNullOrWhiteSpace(wmfBlock.Source))
					{
						newBlock.Source = wmfBlock.Source;
					}
				}
				else if (localBlocks.TryGetValue(wmfBlock.Address, out var localBlock))
				{
					if (wmfBlock.Expiry > localBlock.Expiry && localBlock.User is not null)
					{
						updateCount++;
						newBlock = new CommonBlock(localBlock.User.Name, wmfBlock.Expiry, wmfBlock.Source);
						newBlocks.Add(newBlock.Address, newBlock);
					}
				}
				else
				{
					newCount++;
					newBlocks.Add(wmfBlock.Address, wmfBlock);
				}
			}
		}

		return new BlockUpdates(overlapCount, updateCount, newCount, newBlocks);
	}

	private static DateTime GetStartTime(string botName, Dictionary<string, Block> localBlocks)
	{
		var paddedNow = DateTime.Now.AddHours(-4); // Don't count anything within the last four hours, since that's almost certainly part of an aborted run of the current job rather than a previous job.
		var lastRun = DateTime.MinValue;
		foreach (var block in localBlocks.Values)
		{
			if (block.StartTime < paddedNow &&
				block.StartTime > lastRun &&
				(block.BlockedBy?.Name).OrdinalEquals(botName) &&
				(block.Reason?.Contains("proxy", StringComparison.OrdinalIgnoreCase) ?? false))
			{
				lastRun = block.StartTime;
			}
		}

		// -2 hours to allow plenty of time for possible mis-synchronization due to job run times, time differences, etc.
		if (lastRun != DateTime.MinValue)
		{
			lastRun = lastRun.AddHours(-2);
		}

		return lastRun;
	}

	private static (Regex BlockFilterRegex, List<string> Exceptions) ParseBlocksFilter()
	{
		var blocksFile = File.ReadAllLines("Jobs\\ImportBlocksFilter.txt");
		var regex = new Regex(blocksFile[0], RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
		var exceptionsText = new List<string>(blocksFile[1..]);
		return (regex, exceptionsText);
	}
	#endregion

	#region Private Methods
	private void AddEnWikiBlocks(IMediaWikiClient client, List<CommonBlock> blocks, DateTime lastRun)
	{
		var uri = new Uri("https://en.wikipedia.org/w/api.php");
		var wmApi = new WikiAbstractionLayer(client, uri);
		wmApi.SendingRequest += JobManager.WalSendingRequest;
		var wmSite = new Site(wmApi);
		wmSite.Login(null, null);
		var input = new BlocksInput
		{
			Start = lastRun == DateTime.MinValue ? null : lastRun,
			SortAscending = true,
			FilterAccount = Filter.Exclude,
			Properties =
				BlocksProperties.User |
				BlocksProperties.Expiry |
				BlocksProperties.Reason
		};

		var result = wmApi.Blocks(input);
		foreach (var block in result)
		{
			if (block.User is not null &&
				block.Reason is not null &&
				IPAddress.TryParse(block.User, out var addr) &&
				!this.IsException(addr) &&
				this.blockFilter.IsMatch(block.Reason))
			{
				blocks.Add(CommonBlock.FromReason(block.User, block.Expiry, block.Reason));
			}
		}

		wmApi.SendingRequest -= JobManager.WalSendingRequest;
	}

	private void AddGlobalBlocks(IMediaWikiClient client, List<CommonBlock> blocks, DateTime lastRun)
	{
		var uri = new Uri("https://meta.wikimedia.org/w/api.php");
		var wmApi = new WikiAbstractionLayer(client, uri);
		wmApi.SendingRequest += JobManager.WalSendingRequest;
		wmApi.Initialize();

		var input = new GlobalBlocksInput
		{
			Start = lastRun == DateTime.MinValue ? null : lastRun,
			SortAscending = true,
			Properties =
				GlobalBlocksProperties.Address |
				GlobalBlocksProperties.Expiry |
				GlobalBlocksProperties.Reason
		};

		var list = new ListGlobalBlocks(wmApi, input);
		var result = wmApi.RunListQuery(list);
		foreach (var block in result)
		{
			if (block.Address is not null &&
				block.Reason is not null &&
				IPAddress.TryParse(block.Address, out var addr) &&
				!this.IsException(addr) &&
				this.blockFilter.IsMatch(block.Reason))
			{
				blocks.Add(CommonBlock.FromReason(block.Address, block.Expiry, block.Reason));
			}
		}

		wmApi.SendingRequest -= JobManager.WalSendingRequest;
	}

	private IEnumerable<IPAddressRange> GetCloudflareExceptions()
	{
		var uri = new Uri("https://www.cloudflare.com/ips-v4/#");
		if (this.client.Get(uri) is string exceptionsText)
		{
			foreach (var exception in exceptionsText.Split('\n'))
			{
				var ipRange = IPAddressRange.Parse(exception);
				yield return ipRange;
			}
		}
	}

	private Dictionary<string, Block> GetLocalBlocks(Site site)
	{
		var localBlocks = site.LoadBlocks(Filter.Exclude, Filter.Any, Filter.Any, Filter.Any);
		var retval = new Dictionary<string, Block>(StringComparer.Ordinal);
		foreach (var block in localBlocks)
		{
			if (block.User?.Name is string addressText &&
				IPAddress.TryParse(addressText, out var addr))
			{
				if (!this.IsException(addr))
				{
					if (block.Reason is not null && this.blockFilter.IsMatch(block.Reason))
					{
						retval.Add(block.User.Name, block);
					}
				}
				else
				{
					// This should rarely happen—if ever—so for now, we're just unblocking while blocks are being loaded. That's really a crappy way to do it, though, so should be moved to something with a progress bar if, for some reason, it becomes a more common occurrence.
					var input = new UnblockInput(addressText)
					{
						Reason = "Unblock Cloudflare"
					};

					this.Site.AbstractionLayer.Unblock(input);
				}
			}
		}

		return retval;
	}

	private List<CommonBlock> GetWmfBlocks(DateTime lastRun)
	{
		var wmfBlocks = new List<CommonBlock>();
		this.StatusWriteLine("Getting WMF global blocks");
		this.AddGlobalBlocks(this.client, wmfBlocks, lastRun);
		this.StatusWriteLine("Getting English wiki blocks");
		this.AddEnWikiBlocks(this.client, wmfBlocks, lastRun);

		return wmfBlocks;
	}

	private bool IsException(IPAddress addr)
	{
		foreach (var exception in this.exceptions)
		{
			if (exception.Contains(addr))
			{
				return true;
			}
		}

		return false;
	}

	private void UpdateBlocks(Site site, IDictionary<string, CommonBlock> newBlocks)
	{
		this.ResetProgress(newBlocks.Count);
		foreach (var newBlock in newBlocks)
		{
			var block = newBlock.Value;
			var reason = "Webhost/proxy";
			if (block.Source is not null)
			{
				reason += ": " + block.Source;
			}

			var user = new User(site, block.Address);
			try
			{
				var result = user.Block(reason, BlockFlags.AnonymousOnly | BlockFlags.NoCreate, block.Expiry, true);
			}
			catch
			{
			}

			this.Progress++;
		}
	}
	#endregion

	#region Private Records
	private record struct BlockUpdates(int OverlapCount, int UpdateCount, int NewCount, IDictionary<string, CommonBlock> NewBlocks);
	#endregion

	#region Private Classes
	private sealed class CommonBlock
	{
		public CommonBlock(string address, DateTime? expiry, string? source)
		{
			ArgumentNullException.ThrowIfNull(address);
			this.Address = address;
			this.Expiry = expiry;
			this.Source = source;
		}

		public string Address { get; }

		public DateTime? Expiry { get; set; }

		public string? Source { get; set; }

		public static CommonBlock FromReason(string address, DateTime? expiry, string? reason)
		{
			string? source = null;
			if (reason is not null)
			{
				var commentOffset = reason.LastIndexOf("<!--", StringComparison.Ordinal);
				if (commentOffset != -1)
				{
					commentOffset += 4;
					var endOffset = reason.IndexOf("-->", commentOffset, StringComparison.Ordinal);
					if (endOffset >= 0)
					{
						source = reason[commentOffset..endOffset].Trim();
					}
				}
			}

			return new CommonBlock(address, expiry, source);
		}
	}
	#endregion
}
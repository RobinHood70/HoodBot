namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Wikimedia;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Import Blocks")]
	internal sealed class ImportBlocks(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
	{
		#region Static Fields
		private static readonly Regex BlockFilter;
		private static readonly Regex Ipv4Check = new(@"\A\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?<range>\/\d+)?\Z", RegexOptions.CultureInvariant, Globals.DefaultRegexTimeout);
		#endregion

		#region Static Constructor
		static ImportBlocks()
		{
			// Regex initialized from file to keep specific filters hidden from vandals.
			var filter = File.ReadAllText("Jobs\\ImportBlocksFilter.txt");
			BlockFilter = new(filter, RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
		}
		#endregion

		/*
		#region Public Static Methods
		public static void DebugRequest(IWikiAbstractionLayer sender, RequestEventArgs eventArgs) => Debug.WriteLine($"Temporary API Request: {eventArgs.Request}");
		#endregion
		*/

		#region Protected Override Methods
		protected override void Main()
		{
			if (this.Site.User is null)
			{
				throw new InvalidOperationException("This job cannot be run anonymously.");
			}

			var localBlocks = this.GetLocalBlocks();
			DateTime? lastRun = DateTime.MinValue;
			var dtPad = DateTime.Now.AddHours(-4); // Don't count anything within the last four hours, since that's almost certainly part of an aborted run of the current job rather than a previous job.
			var botName = this.Site.User.Name;
			foreach (var block in localBlocks.Values)
			{
				if (block.StartTime < dtPad &&
					block.StartTime > lastRun &&
					string.Equals(block.BlockedBy?.Name, botName, StringComparison.Ordinal) &&
					(block.Reason?.Contains("proxy", StringComparison.OrdinalIgnoreCase) ?? false))
				{
					lastRun = block.StartTime;
				}
			}

			// -2 hours to allow plenty of time for possible mis-synchronization due to job run times, time differences, etc.
			lastRun = lastRun == DateTime.MinValue ? null : lastRun.Value.AddHours(-2);

			var wmfBlocks = new List<CommonBlock>();
			this.GetGlobalBlocks(wmfBlocks, lastRun);
			this.GetEnWikiBlocks(wmfBlocks, lastRun);

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

			this.StatusWriteLine($"Applying {updateCount} updated and {newCount} new blocks ({overlapCount} overlaps)");
			this.ProgressMaximum = newBlocks.Count;
			foreach (var newBlock in newBlocks)
			{
				var block = newBlock.Value;
				var reason = "Webhost/proxy";
				if (block.Source is not null)
				{
					reason += ": " + block.Source;
				}

				var user = new User(this.Site, block.Address);
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

		#region Private Methods
		private void GetEnWikiBlocks(List<CommonBlock> blocks, DateTime? lastRun)
		{
			this.StatusWriteLine("Getting English wiki blocks");
			var api = (WikiAbstractionLayer)this.Site.AbstractionLayer;
			var client = api.Client;
			var uri = new Uri("https://en.wikipedia.org/w/api.php");
			var wmApi = new WikiAbstractionLayer(client, uri);
			//// wmApi.SendingRequest += DebugRequest;
			var wmSite = new Site(wmApi);
			wmSite.Login(null, null);
			var input = new BlocksInput
			{
				Start = lastRun,
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
				if (block.User is not null)
				{
					if (Ipv4Check.Match(block.User).Success &&
						BlockFilter.IsMatch(block.Reason ?? string.Empty))
					{
						blocks.Add(CommonBlock.FromReason(block.User, block.Expiry, block.Reason));
					}
				}
			}

			// wmApi.SendingRequest -= DebugRequest;
		}

		private void GetGlobalBlocks(List<CommonBlock> blocks, DateTime? lastRun)
		{
			this.StatusWriteLine("Getting WMF global blocks");
			var api = (WikiAbstractionLayer)this.Site.AbstractionLayer;
			var client = api.Client;
			var uri = new Uri("https://meta.wikimedia.org/w/api.php");
			//// var uri = new Uri("https://en.wikipedia.org/w/api.php");
			var wmApi = new WikiAbstractionLayer(client, uri);
			//// wmApi.SendingRequest += DebugRequest;
			var input = new GlobalBlocksInput
			{
				Start = lastRun,
				SortAscending = true,
				Properties =
					GlobalBlocksProperties.Address |
					GlobalBlocksProperties.Expiry |
					GlobalBlocksProperties.Reason
			};
			var list = new ListGlobalBlocks(wmApi, input);
			var result = wmApi.RunModuleQuery(list);
			foreach (var block in result)
			{
				if (block.Address is not null)
				{
					if (Ipv4Check.Match(block.Address).Success &&
						BlockFilter.IsMatch(block.Reason ?? string.Empty))
					{
						blocks.Add(CommonBlock.FromReason(block.Address, block.Expiry, block.Reason));
					}
				}
			}

			// wmApi.SendingRequest -= DebugRequest;
		}

		private Dictionary<string, Block> GetLocalBlocks()
		{
			this.StatusWriteLine("Getting local blocks");
			var localBlocks = this.Site.LoadBlocks(Filter.Exclude, Filter.Any, Filter.Any, Filter.Any);
			var retval = new Dictionary<string, Block>(StringComparer.Ordinal);
			foreach (var block in localBlocks)
			{
				if (block.User is not null)
				{
					retval.Add(block.User.Name, block);
				}
			}

			return retval;
		}
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
}
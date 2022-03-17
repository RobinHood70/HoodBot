namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.IO;
	using System.Net;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;

	public class InterwikiVersions : WikiJob
	{
		[JobInfo("Interwiki Versions", "External")]
		public InterwikiVersions(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void Main()
		{
			SimpleClient client = (SimpleClient)((WikiAbstractionLayer)this.Site.AbstractionLayer).Client;
			client.HonourMaxLag = false;
			client.Retries = 3;
			this.ProgressMaximum = this.Site.InterwikiMap.Count;
			foreach (var entry in this.Site.InterwikiMap)
			{
				if (!entry.LocalFarm && entry.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					var originalPath = entry.Path;
					var path = originalPath.TrimEnd('/');
					if (!path.EndsWith("$1", StringComparison.Ordinal))
					{
						this.StatusWriteLine($"URL not wiki-like: {path}");
						continue;
					}

					path = path[0..^2];
					path = path.Split('?', 2)[0];
					Uri uri = new(path);
					SiteCapabilities capabilities = new(client);
					try
					{
						if (capabilities.Get(uri))
						{
							this.StatusWriteLine($"Found: {capabilities.SiteName}\t{capabilities.Version}\t{originalPath} (API: {capabilities.Api})");
						}
						else
						{
							this.StatusWriteLine(capabilities.ErrorMessage ?? $"Does not appear to be a wiki: {path}");
						}
					}
					catch (IOException)
					{
						this.StatusWriteLine($"Error response: {path}");
					}
					catch (WebException e)
					{
						this.StatusWriteLine($"Error response: {path} {e.Message}");
					}
					catch (InvalidDataException)
					{
						this.StatusWriteLine($"May be a wiki, but if so, API is blocked: {path}");
					}
				}

				this.Progress++;
			}
		}
	}
}

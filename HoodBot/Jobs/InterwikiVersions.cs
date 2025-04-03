namespace RobinHood70.HoodBot.Jobs;

using System;
using System.IO;
using System.Net;
using RobinHood70.Robby;
using RobinHood70.WallE.Clients;
using RobinHood70.WallE.Eve;

[method: JobInfo("Interwiki Versions", "External")]
internal sealed class InterwikiVersions(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var client = (SimpleClient)((WikiAbstractionLayer)this.Site.AbstractionLayer).Client;
		client.Retries = 3;
		this.ProgressMaximum = this.Site.InterwikiMap.Count;
		foreach (var entry in this.Site.InterwikiMap)
		{
			if (!entry.LocalFarm && entry.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				var originalPath = entry.Path;
				var path = originalPath.TrimEnd('/');
				this.StatusWrite(path + '\t');
				if (!path.EndsWith("$1", StringComparison.Ordinal))
				{
					this.StatusWriteLine($"URL not wiki-like");
					continue;
				}

				path = path[0..^2];
				path = path.Split('?', 2)[0];
				Uri uri = new(path);
				string capabilities;
				try
				{
					capabilities = GetSiteCapabilities(client, uri);
				}
				catch (System.Net.Http.HttpRequestException)
				{
					capabilities = "Unknown error";
				}

				this.StatusWriteLine(capabilities);
			}

			this.Progress++;
		}
	}
	#endregion

	#region Private Static Methods
	private static string GetSiteCapabilities(SimpleClient client, Uri uri)
	{
		var capabilities = new SiteCapabilities(client);
		try
		{
			return capabilities.Get(uri)
				? $"Found: {capabilities.SiteName}\t{capabilities.Version} (API: {capabilities.Api})"
				: capabilities.ErrorMessage ?? $"Does not appear to be a wiki";
		}
		catch (IOException)
		{
			return $"Error response";
		}
		catch (WebException e)
		{
			return $"Error response: {e.Message}";
		}
		catch (InvalidDataException)
		{
			return $"May be a wiki, but if so, API is blocked";
		}
		catch (Exception e)
		{
			return $"Unknown error querying {capabilities.SiteName}: {e.Message}";
		}
	}
	#endregion
}
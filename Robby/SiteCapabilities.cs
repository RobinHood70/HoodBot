namespace RobinHood70.Robby
{
	using System;
	using System.Net;
	using System.Text.RegularExpressions;
	using System.Xml.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Methods by which the wiki can be accessed.</summary>
	public enum EntryPoint
	{
		/// <summary>No access point was found.</summary>
		None,

		/// <summary>Index.php access.</summary>
		Index,

		/// <summary>API access.</summary>
		Api
	}

	/// <summary>Represents what the site is capable of at a basic level.</summary>
	public class SiteCapabilities
	{
		#region Static Fields
		private static readonly Regex FindRsdLink = new Regex(@"<link rel=""EditURI"" .*?href=""(?<rsdlink>.*?)""", RegexOptions.Compiled);
		private static readonly Regex FindScript = new Regex(@"<script>.*?(wgScriptPath=""(?<scriptpath>.*?)"".*?|wgServer=""(?<serverpath>.*?)"".*?)+</script>", RegexOptions.Singleline | RegexOptions.Compiled);
		private static readonly Regex FindPhpLink = new Regex(@"href=""(?<scriptpath>/([!#$&-;=?-\[\]_a-z~]|%[0-9a-fA-F]{2})+?)?/(api|index).php");
		#endregion

		#region Fields
		private readonly IMediaWikiClient client;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteCapabilities"/> class.</summary>
		/// <param name="client">The <see cref="IMediaWikiClient"/> client to be used to access the site.</param>
		public SiteCapabilities(IMediaWikiClient client)
		{
			ThrowNull(client, nameof(client));
			this.client = client;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the Uri to the API entry point.</summary>
		/// <value>The API entry point.</value>
		public Uri? Api { get; private set; }

		/// <summary>Gets the current user.</summary>
		/// <value>The current user, if any; otherwise, null.</value>
		public string? CurrentUser { get; private set; }

		/// <summary>Gets the Uri to the index.php entry point.</summary>
		/// <value>The index.php entry point.</value>
		public Uri? Index { get; private set; }

		/// <summary>Gets the entry points for read access.</summary>
		/// <value>The entry points for read access.</value>
		public EntryPoint ReadEntryPoint { get; private set; }

		/// <summary>Gets the name of the wiki.</summary>
		/// <value>The name of the wiki.</value>
		public string? SiteName { get; private set; }

		/// <summary>Gets a value indicating whether the site supports the <c>maxlag</c> parameter for speed throttling.</summary>
		/// <value><see langword="true"/> if the site supports <c>maxlag</c>; otherwise, <see langword="false"/>.</value>
		public bool SupportsMaxLag { get; private set; }

		/// <summary>Gets the entry points for write access.</summary>
		/// <value>The entry points for write access.</value>
		public EntryPoint WriteEntryPoint { get; private set; }
		#endregion

		#region Public Methods

		/// <summary>Gets all relevant information from the site.</summary>
		/// <param name="anyPage">Any page on the wiki.</param>
		/// <returns><see langword="true"/> if the wiki capabailities were successfully loaded; otherwise <see langword="false"/>.</returns>
		/// <remarks>This can be called multiple times with different URIs to get information for different wikis. Previous information will be cleared with each new call.</remarks>
		public bool Get(Uri anyPage)
		{
			// TODO: Convert to use URIs and related objects instead of strings whenever possible.
			ThrowNull(anyPage, nameof(anyPage));

			this.Clear();
			var fullHost = new UriBuilder(anyPage.Scheme, anyPage.Host).Uri;
			var tryPath = anyPage.AbsolutePath;
			Uri? tryLoc = null;
			var offset = tryPath.IndexOf("/index.php", StringComparison.Ordinal);
			if (offset == -1)
			{
				offset = tryPath.IndexOf("/api.php", StringComparison.Ordinal);
			}

			if (offset >= 0)
			{
				var urib = new UriBuilder(fullHost)
				{
					Path = tryPath.Replace("index.php", "api.php").Substring(0, offset + 8)
				};
				tryLoc = urib.Uri;
				tryPath = tryPath.Substring(0, offset + 1);
			}

			if (!tryPath.EndsWith("/", StringComparison.Ordinal))
			{
				/* If it doesn't look like a php page or blank path, try various methods of figuring out the php locations. */
				var pageData = this.TryGet(anyPage);
				if (pageData == null)
				{
					// Web page given could not be accessed, so abort.
					return false;
				}

				var rsdLink = FindRsdLink.Match(pageData);
				if (rsdLink.Success)
				{
					var rsdLinkFixed = rsdLink.Groups["rsdlink"].Value;
					if (rsdLinkFixed.StartsWith("//", StringComparison.Ordinal))
					{
						rsdLinkFixed = anyPage.Scheme + ':' + rsdLinkFixed;
					}

					var rsdInfo = this.client.Get(new Uri(rsdLinkFixed));
					var rsd = XDocument.Parse(rsdInfo);
					var ns = rsd.Root.GetDefaultNamespace();
					foreach (var descendant in rsd.Descendants(ns + "api"))
					{
						if ((bool)descendant.Attribute("preferred"))
						{
							var apiLink = WebUtility.HtmlDecode((string)descendant.Attribute("apiLink"));
							tryLoc = new Uri(apiLink);
							tryPath = apiLink.Substring(0, apiLink.LastIndexOf('/') + 1);
							break;
						}
					}
				}
				else
				{
					var foundScript = FindScript.Match(pageData);
					if (foundScript.Success)
					{
						// Should occur only in 1.16
						tryPath = foundScript.Groups["serverpath"].Value + foundScript.Groups["scriptpath"].Value + '/';
						tryLoc = new Uri(tryPath + "api.php");
					}
					else
					{
						var foundPhpLink = FindPhpLink.Match(pageData);
						if (foundPhpLink.Success)
						{
							tryPath = fullHost + foundPhpLink.Groups["scriptpath"].Value + '/';
							tryLoc = new Uri(tryPath + "api.php");
						}
					}
				}
			}

			if (tryLoc != null)
			{
				try
				{
					// Something above gave us a tentative api.php link, so try it.
					var api = new WikiAbstractionLayer(this.client, tryLoc);
					if (api.IsEnabled())
					{
						api.Initialize();
						this.Api = api.Uri;
						Uri? index = null;
						if (!string.IsNullOrWhiteSpace(api.Script))
						{
							index = new UriBuilder(fullHost)
							{
								Path = api.Script
							}.Uri;
						}

						this.Index = index;
						this.SiteName = api.SiteName;
						this.ReadEntryPoint = EntryPoint.Api;
						this.SupportsMaxLag = api.SupportsMaxLag;
						this.CurrentUser = api.UserId == 0 ? null : api.UserName;
						this.WriteEntryPoint =
							api.Flags.HasFlag(SiteInfoFlags.WriteApi) ? EntryPoint.Api :
							this.Index == null ? EntryPoint.None :
							EntryPoint.Index;

						// API gave us everything we need, so skip trying index.php.
						return true;
					}
				}
				catch (InvalidOperationException)
				{
				}
			}

			// Last resort
			tryLoc = new Uri(tryPath + "index.php?maxlag=-1");

			// We don't care about the result, only whether it's a valid link.
			this.client.RequestingDelay += this.Client_RequestingDelay;
			if (this.TryGet(tryLoc) != null)
			{
				this.Index = tryLoc;
				this.ReadEntryPoint = EntryPoint.Index;
				this.WriteEntryPoint = EntryPoint.Index;

				// TODO: Add more information retrieval for index.php if abstraction layer is ever written for it.
				return true;
			}

			this.client.RequestingDelay -= this.Client_RequestingDelay;

			return false;
		}
		#endregion

		#region Private Methods
		private void Clear()
		{
			this.Api = null;
			this.CurrentUser = null;
			this.Index = null;
			this.ReadEntryPoint = EntryPoint.None;
			this.SiteName = null;
			this.SupportsMaxLag = false;
			this.WriteEntryPoint = EntryPoint.None;
		}

		private void Client_RequestingDelay(IMediaWikiClient sender, DelayEventArgs eventArgs)
		{
			if (eventArgs.Reason == DelayReason.MaxLag)
			{
				eventArgs.Cancel = true;
				this.SupportsMaxLag = true;
			}
		}

		private string? TryGet(Uri uri)
		{
			try
			{
				return this.client.Get(uri);
			}
			catch (WebException)
			{
				return null;
			}
		}
		#endregion
	}
}
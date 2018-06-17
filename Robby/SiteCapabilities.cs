namespace RobinHood70.Robby
{
	using System;
	using System.Net;
	using System.Text.RegularExpressions;
	using System.Web;
	using System.Xml.Linq;
	using WallE.Base;
	using WallE.Clients;
	using WallE.Eve;
	using static WikiCommon.Globals;

	/// <summary>Methods by which the wiki can be accessed.</summary>
	public enum EntryPoint
	{
		/// <summary>Index.php access.</summary>
		Index,

		/// <summary>API access.</summary>
		Api
	}

	/// <summary>Represents what the site is capable of at a basic level.</summary>
	public class SiteCapabilities
	{
		#region Static Fields
		private static Regex findRsdLink = new Regex(@"<link rel=""EditURI"" .*?href=""(?<rsdlink>.*?)""", RegexOptions.Compiled);
		private static Regex findScript = new Regex(@"<script>.*?(wgScriptPath=""(?<scriptpath>.*?)"".*?|wgServer=""(?<serverpath>.*?)"".*?)+</script>", RegexOptions.Singleline | RegexOptions.Compiled);
		private static Regex findPhpLink = new Regex(@"href=""(?<scriptpath>/([!#$&-;=?-\[\]_a-z~]|%[0-9a-fA-F]{2})+?)?/(api|index).php");
		#endregion

		#region Constructors
		private SiteCapabilities()
		{
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the Uri to the API entry point.</summary>
		/// <value>The API entry point.</value>
		public Uri Api { get; private set; }

		/// <summary>Gets the Uri to the index.php entry point.</summary>
		/// <value>The index.php entry point.</value>
		public Uri Index { get; private set; }

		/// <summary>Gets the entry points for read access.</summary>
		/// <value>The entry points for read access.</value>
		public EntryPoint ReadEntryPoint { get; private set; }

		/// <summary>Gets the entry points for write access.</summary>
		/// <value>The entry points for write access.</value>
		public EntryPoint WriteEntryPoint { get; private set; }
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="SiteCapabilities"/> class and gets all relevant information from the site.</summary>
		/// <param name="client">The <see cref="IMediaWikiClient"/> client to be used to access the site.</param>
		/// <param name="anyPage">Any page on the wiki.</param>
		/// <returns>A new instance of the <see cref="SiteCapabilities"/> class with information about the site's capabilities.</returns>
		public static SiteCapabilities Get(IMediaWikiClient client, Uri anyPage)
		{
			ThrowNull(client, nameof(client));
			ThrowNull(anyPage, nameof(anyPage));
			var result = new SiteCapabilities();
			var fullHost = anyPage.Scheme + "://" + anyPage.Host;
			var tryPath = anyPage.AbsoluteUri;
			string tryLoc = null;
			var offset = tryPath.IndexOf("/index.php", StringComparison.Ordinal);
			if (offset == -1)
			{
				offset = tryPath.IndexOf("/api.php", StringComparison.Ordinal);
			}

			if (offset >= 0)
			{
				tryPath = tryPath.Substring(0, offset + 1);
			}

			if (!tryPath.EndsWith("/", StringComparison.Ordinal))
			{
				/* If it doesn't look like a php page or blank path, try various methods of figuring out the php locations. */
				var pageData = client.Get(anyPage);
				var rsdLink = findRsdLink.Match(pageData);
				if (rsdLink.Success)
				{
					var rsdLinkFixed = rsdLink.Groups["rsdlink"].Value;
					if (rsdLinkFixed.StartsWith("//", StringComparison.Ordinal))
					{
						rsdLinkFixed = anyPage.Scheme + ':' + rsdLinkFixed;
					}

					var rsdInfo = client.Get(new Uri(rsdLinkFixed));
					var rsd = XDocument.Parse(rsdInfo);
					var ns = rsd.Root.GetDefaultNamespace();
					foreach (var descendant in rsd.Descendants(ns + "api"))
					{
						if ((bool)descendant.Attribute("preferred"))
						{
							tryLoc = (string)descendant.Attribute("apiLink");
							tryLoc = HttpUtility.HtmlDecode(tryLoc);
							tryPath = tryLoc.Substring(0, tryLoc.LastIndexOf('/'));
							break;
						}
					}
				}
				else
				{
					var foundScript = findScript.Match(pageData);
					if (foundScript.Success)
					{
						// Should occur only in 1.16
						tryPath = foundScript.Groups["serverpath"].Value + foundScript.Groups["scriptpath"].Value + '/';
						tryLoc = tryPath + "api.php";
					}
					else
					{
						var foundPhpLink = findPhpLink.Match(pageData);
						if (foundPhpLink.Success)
						{
							tryPath = fullHost + foundPhpLink.Groups["scriptpath"].Value + '/';
							tryLoc = tryPath + "api.php";
						}
					}
				}
			}

			if (tryLoc != null)
			{
				// Something above gave us a tentative api.php link, so try it.
				var api = new WikiAbstractionLayer(client, new Uri(tryLoc));
				if (api.IsEnabled())
				{
					result.Api = new Uri(tryPath);
					result.Index = new Uri(fullHost + api.Script);
					result.ReadEntryPoint = EntryPoint.Api;
					if (api.Flags.HasFlag(SiteInfoFlags.WriteApi))
					{
						result.WriteEntryPoint = EntryPoint.Api;
					}

					// API gave us everything we need, so skip trying index.php.
					return result;
				}
			}

			// Last resort
			tryLoc = tryPath + "index.php";
			try
			{
				client.Get(new Uri(tryLoc)); // We don't care about the result at this point, only whether it's a valid link.

				// If no exception was caught, then the link appears to be good, so set it. Other values should still be set to default, and can stay that way.
				result.Index = new Uri(tryPath);
			}
			catch (WebException)
			{
			}

			return result;
		}
		#endregion
	}
}
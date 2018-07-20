namespace RobinHood70.Testing
{
	using System;
	using System.Diagnostics;
	using System.Security.Cryptography;
	using System.Text;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public static class TestingCommon
	{
		#region Debug Methods
#if DEBUG
		/// <summary>A convenience method for debugging, this simply outputs all warnings to any Debug trace listeners (e.g., the Debug Output window).</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="WarningEventArgs"/> instance containing the event data.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "RobinHood70.Robby.Globals.CurrentCulture(System.String,System.Object[])", Justification = "I'm allowing English only here because it's only intended for debugging.")]
		public static void DebugWarningEventHandler(Robby.Site sender, Robby.WarningEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine(CurrentCulture($"Warning ({e.Sender.GetType()}): {e.Warning}"), sender.ToString());
		}

		public static void DebugWarningEventHandler(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine($"Warning ({e.Warning.Code}): {e.Warning.Info}", sender.ToString());
		}

		public static void DebugResponseEventHandler(IWikiAbstractionLayer sender, ResponseEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine(e.Response, sender.ToString());
		}

		public static void DebugShowDelay(IMediaWikiClient sender, DelayEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			var aborted = string.Empty;

			if (e.Cancel)
			{
				aborted = "Aborted ";
			}

			Debug.WriteLine($"{aborted}Delay: {e.DelayTime} milliseconds. Reason: {e.Reason}", sender.ToString());
		}

		public static void DebugShowRequest(IWikiAbstractionLayer sender, RequestEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine("{0} - {1}", sender.UserName, e.Request);
		}
#endif
		#endregion

		#region Public Methods
		public static WikiAbstractionLayer GetAbstractionLayer(WikiInfo info, bool useAdmin)
		{
			IMediaWikiClient baseClient = new SimpleClient(useAdmin ? info.AdminUserName : info.UserName, @"D:\Data\WallE\" + (useAdmin ? "cookiesAdmin" : "cookies") + ".dat") { Name = useAdmin ? "Admin" : "Normal" };
			var client = (info.ReadInterval == 0 && info.WriteInterval == 0)
				? baseClient
				: new ThrottledClient(baseClient, TimeSpan.FromMilliseconds(info.ReadInterval), TimeSpan.FromMilliseconds(info.WriteInterval));
			client.RequestingDelay += DebugShowDelay;

			var wal = new WikiAbstractionLayer(client, info.Uri) { Assert = null };
			wal.StopCheckMethods &= ~StopCheckMethods.Assert;

			return wal;
		}

		public static void RunJobs(WikiAbstractionLayer adminClient, string secretKey)
		{
			var path = adminClient.GetArticlePath(string.Empty);
			var request = new Request(path, RequestType.Post, false);
			var message = request
				.Add("async", true)
				.Add("maxjobs", 1000)
				.Add("sigexpiry", (int)(DateTime.UtcNow.AddSeconds(5) - new DateTime(1970, 1, 1)).TotalSeconds)
				.Add("tasks", "jobs")
				.Add("title", "Special:RunJobs")
				.ToString();
			message = message.Substring(message.IndexOf('?') + 1);
			request.Add("signature", GetHmac(message, secretKey));
			adminClient.SendRequest(request);
		}
		#endregion

		#region Private Methods
		private static string GetHmac(string message, string key)
		{
			var sb = new StringBuilder(64);
			var encoding = Encoding.UTF8;
			var keyBytes = encoding.GetBytes(key);
			var messageBytes = encoding.GetBytes(message);
			byte[] hash;
			using (var hmacsha1 = new HMACSHA1(keyBytes))
			{
				hash = hmacsha1.ComputeHash(messageBytes);
			}

			foreach (var b in hash)
			{
				sb.Append(b.ToString("X2"));
			}

			return sb.ToString().ToLowerInvariant();
		}
		#endregion
	}
}
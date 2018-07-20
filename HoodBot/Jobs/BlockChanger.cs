namespace RobinHood70.HoodBot.Jobs
{
	public class BlockChanger
	{
		// TODO: Rewrite this as a job.

		/*
		private void BlockChanger(object sender, EventArgs e)
		{
		const int NumYears = 1;

		this.ButtonQuick.Enabled = false;
		var wiki = wikiInfo;
		this.DoGlobalSetup(wiki.Uri, wiki.UserName, wiki.Password, true);
		Global.wiki.ClearHasMessage();

		try
		{
		var blocksInput = new BlocksInput();
		blocksInput.Properties = BlocksProperties.Expiry | BlocksProperties.Flags | BlocksProperties.Reason | BlocksProperties.Timestamp | BlocksProperties.User;
		blocksInput.ShowAccount = false;
		blocksInput.ShowIP = true;
		blocksInput.ShowRange = false;
		blocksInput.ShowTemp = false;

		var comparer = CultureInfo.InvariantCulture.CompareInfo;

		var blocks = Global.wiki.BlocksLoad(blocksInput);
		foreach (var block in blocks)
		{
		if (comparer.IndexOf(block.Reason, "proxy", CompareOptions.IgnoreCase) >= 0 && comparer.IndexOf(block.Reason, "tor", CompareOptions.IgnoreCase) == -1)
		{
		continue;
		}

		if (block.Timestamp.Value <= DateTime.Now.AddYears(-NumYears))
		{
		var unblock = new UserUnblockInput(block.User);
		unblock.Reason = "Remove infinite IP block";
		Global.wiki.UserUnblock(unblock);
		}
		else
		{
		var newBlock = new UserBlockInput(block.User);
		newBlock.AllowUserTalk = block.AllowUserTalk;
		newBlock.AnonymousOnly = block.AnonymousOnly;
		newBlock.AutoBlock = block.AutoBlock;
		newBlock.Expiry = block.Timestamp.Value.AddYears(NumYears);
		newBlock.NoCreate = block.NoCreate;
		newBlock.NoEmail = false;
		newBlock.Reason = "Re-block with finite block length";
		newBlock.Reblock = true;
		newBlock.User = block.User;

		Global.wiki.UserBlock(newBlock);
		}

		FormTestBed.CheckTalkPage();
		}
		}
		catch (WikiException ex)
		{
		MessageBox.Show(ex.ErrorInfo, ex.ErrorCode);
		}

		Global.wiki.SiteLogout();
		this.ButtonQuick.Enabled = true;
		}

		private static void CheckTalkPage()
		{
		var userInfo = Global.wiki.UserGetInfo(new UserInfoInput(UserInfoProperties.HasMsg));
		}
		*/
	}
}

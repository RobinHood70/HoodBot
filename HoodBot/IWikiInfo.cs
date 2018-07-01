namespace RobinHood70.HoodBot
{
	using System;

	public interface IWikiInfo
	{
		Uri Api { get; set; }

		string DisplayName { get; set; }

		string Password { get; set; }

		int ReadThrottling { get; set; }

		string UserName { get; set; }

		int WriteThrottling { get; set; }
	}
}
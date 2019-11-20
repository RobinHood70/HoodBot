namespace RobinHood70.HoodBot.Models
{
	using System;

	[Flags]
	public enum JobTypes
	{
		/*
		Currently only supporting simple Read/Write flags, but could be expanded to distinguish between different types of jobs, for example:
			* PageEdit (anything that edits pages as opposed to moving them or whatever)
			* Report (for single-page reports)
			* User (anything that works only on users or in user space)
			... etc.
		*/
		None = 0,
		Read = 1,
		Write = 1 << 1,
	}
}

namespace RobinHood70.HoodBot.Uesp
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class HoodBotFunctions : UserFunctions
	{
		public HoodBotFunctions(Site site)
			: base(site)
		{
		}

		public static UserFunctions CreateInstance(Site site) => new HoodBotFunctions(site);

		public override void BeginLogEntry()
		{
			if (this.Site.AllowEditing)
			{
				this.LogPage.Load();
			}
		}

		public override void EndLogEntry() => base.EndLogEntry();
	}
}

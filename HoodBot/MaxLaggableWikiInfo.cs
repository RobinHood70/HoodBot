namespace RobinHood70.HoodBot
{
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;

	public class MaxLaggableWikiInfo : WikiInfo
	{
		#region Public Properties
		// TODO: Add this to forms and saved data. Hard-coded for now.
		public int MaxLag { get; set; } = 5;
		#endregion

		#region Public Override Methods
		public override IWikiAbstractionLayer GetAbstractionLayer(IMediaWikiClient client)
		{
			var retval = base.GetAbstractionLayer(client);
			if (retval is WallE.Eve.WikiAbstractionLayer wal)
			{
				wal.MaxLag = this.MaxLag;
			}

			return retval;
		}
		#endregion
	}
}

namespace RobinHood70.HoodBot.Jobs
{
	public class SaveInfo
	{
		public SaveInfo(string editSummary, bool isMinor)
		{
			this.EditSummary = editSummary;
			this.IsMinor = isMinor;
		}

		public string EditSummary { get; }

		public bool IsMinor { get; }
	}
}

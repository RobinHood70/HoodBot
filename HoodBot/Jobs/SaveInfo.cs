namespace RobinHood70.HoodBot.Jobs
{
	public class SaveInfo
	{
		#region Constructors
		public SaveInfo(string editSummary, bool isMinor)
		{
			this.EditSummary = editSummary;
			this.IsMinor = isMinor;
		}
		#endregion

		#region Public Properties
		public string EditSummary { get; }

		public bool IsMinor { get; }
		#endregion
	}
}

namespace RobinHood70.HoodBot.Jobs.Eso
{
	public class Synergy
	{
		public Synergy(string skill, string text, string synergyLink)
		{
			this.Skill = skill;
			this.Text = text;
			this.SynergyLink = synergyLink;
		}

		public string Skill { get; }

		public string Text { get; }

		public string SynergyLink { get; }
	}
}
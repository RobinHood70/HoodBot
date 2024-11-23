namespace RobinHood70.HoodBot.Jobs.JobModels;

public class Synergy(string skill, string text, string synergyLink)
{
	#region Public Properties
	public string Skill { get; } = skill;

	public string Text { get; } = text;

	public string SynergyLink { get; } = synergyLink;
	#endregion
}
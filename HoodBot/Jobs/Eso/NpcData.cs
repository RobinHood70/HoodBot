namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;

	internal class NPCData
	{
		#region Constructors
		public NPCData(string name, sbyte gender, string npcClass)
		{
			this.Name = name;
			this.Gender = (Gender)gender;
			this.Class = npcClass;
		}
		#endregion

		#region Public Properties
		public string Class { get; }

		public Gender Gender { get; }

		public List<string> Locations { get; } = new List<string>();

		public string Name { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}

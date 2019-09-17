namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;
	using RobinHood70.Robby;

	internal class NPCData
	{
		#region Constructors
		public NPCData(long id, string name, sbyte gender, string npcClass, string pageName)
		{
			this.Id = id;
			this.Name = name;
			this.Gender = (Gender)gender;
			this.Class = npcClass;
			this.PageName = pageName;
		}
		#endregion

		#region Public Properties
		public string Class { get; }

		public Gender Gender { get; }

		public long Id { get; }

		public List<string> Locations { get; } = new List<string>();

		public string Name { get; }

		public Page Page { get; set; }

		public string PageName { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}

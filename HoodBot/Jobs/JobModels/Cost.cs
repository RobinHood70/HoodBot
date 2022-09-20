namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Auto)]
	internal record struct Cost(int Value, int Mechanic)
	{
		#region Public Properties
		public readonly string MechanicText => EsoLog.MechanicNames[this.Mechanic];
		#endregion

		#region Public Override Methods
		public override readonly string ToString() => this.Value == 0
			? "Free"
			: $"{this.Value} {this.MechanicText}";
		#endregion
	}
}

namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Runtime.InteropServices;
	using System.Text;

	[StructLayout(LayoutKind.Auto)]
	internal record struct Cost(string? Value, string? ValuePerTime, string MechanicText)
	{
		#region Public Override Methods
		public override readonly string ToString()
		{
			var sb = new StringBuilder();
			if (this.Value?.Length > 0)
			{
				sb
					.Append(this.Value)
					.Append(' ')
					.Append(this.MechanicText);
			}

			if (this.ValuePerTime?.Length > 0)
			{
				if (this.Value?.Length > 0)
				{
					sb.Append(" + ");
				}

				sb
					.Append(this.ValuePerTime)
					.Append(' ')
					.Append(this.MechanicText)
					.Append(" / 1s");
			}

			return sb.Length == 0
				? "Free"
				: sb.ToString();
		}
		#endregion
	}
}
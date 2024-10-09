namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	internal sealed class Cost(string value, string mechanicText)
	{
		#region Public Properties
		public string MechanicText { get; } = mechanicText;

		public string Value { get; } = value;
		#endregion

		#region Public Static Methods
		public static string GetCostText(IList<Cost> costs) => costs.Count switch
		{
			0 => "Free",
			1 => costs[0].ToString(),
			2 => string.Join(" and ", costs),
			_ => throw new InvalidOperationException("Not yet handled.")
		};
		#endregion

		#region Public Override Methods
		public override string ToString()
		{
			var sb = new StringBuilder();
			if (this.Value is null || this.Value.Length == 0)
			{
				return "Free";
			}

			sb
				.Append(this.Value)
				.Append(' ')
				.Append(this.MechanicText);

			return sb.ToString();
		}
		#endregion
	}
}
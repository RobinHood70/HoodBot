namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Globalization;
	using RobinHood70.CommonCode;

	internal sealed record EsoVersion(int Version, bool Pts)
	{
		#region Public Static Properties
		public static EsoVersion Empty => new(0, false);
		#endregion

		#region Public Properties
		public int ActiveVersion => this.Pts ? this.Version - 1 : this.Version;

		// Two times the version, minus one if it's PTS
		public int SortOrder => this.Version << 1 - (this.Pts ? 1 : 0);

		public string Text => this.Version.ToStringInvariant() + (this.Pts ? "pts" : string.Empty);
		#endregion

		#region Public Static Methods
		public static EsoVersion FromText(string text)
		{
			var pts = false;
			if (text.EndsWith("pts", StringComparison.Ordinal))
			{
				pts = true;
				text = text[..^3];
			}

			var version = int.Parse(text, CultureInfo.InvariantCulture);

			return new EsoVersion(version, pts);
		}
		#endregion
	}
}

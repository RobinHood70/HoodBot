namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Globalization;
using RobinHood70.CommonCode;

internal sealed record EsoVersion(int Version, bool Pts) : IComparable, IComparable<EsoVersion>
{
	#region Public Static Properties
	public static EsoVersion Empty => new(0, false);
	#endregion

	#region Public Properties
	public int ActiveVersion => this.Pts ? this.Version - 1 : this.Version;

	// Two times the version, minus one if it's PTS
	public int SortOrder => this.Pts ? this.Version : this.Version << 1;

	public string Text => this.Version.ToStringInvariant() + (this.Pts ? "pts" : string.Empty);
	#endregion

	#region Operator Overloads
	public static bool operator <(EsoVersion left, EsoVersion right) => Compare(left, right) < 0;

	public static bool operator >(EsoVersion left, EsoVersion right) => Compare(left, right) > 0;

	public static bool operator <=(EsoVersion left, EsoVersion right) => Compare(left, right) <= 0;

	public static bool operator >=(EsoVersion left, EsoVersion right) => Compare(left, right) >= 0;

	#endregion

	#region Public Static Methods
	public static int Compare(EsoVersion left, EsoVersion right) => left is null
		? right is null
			? 0
			: 1
		: left.CompareTo(right);

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

	#region Public Methods
	public int CompareTo(object? obj) => this.CompareTo(obj as EsoVersion);

	public int CompareTo(EsoVersion? other) => other is null
		? 1
		: this.SortOrder.CompareTo(other.SortOrder);
	#endregion
}
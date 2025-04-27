namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Globalization;
using RobinHood70.CommonCode;

public sealed record EsoVersion(int Version, bool Pts) : IComparable, IComparable<EsoVersion>
{
	#region Public Static Properties
	public static EsoVersion Empty => new(0, false);
	#endregion

	#region Public Properties
	public int ActiveVersion => this.Pts ? this.Version - 1 : this.Version;

	public string Text => this.Version == 0
		? string.Empty
		: this.Version.ToStringInvariant() + (this.Pts ? "pts" : string.Empty);
	#endregion

	#region Private Properties

	// Two times the version, minus one if it's PTS
	private int SortOrder => this.Version == 0
		? 0
		: (this.Version << 1) - (this.Pts ? 1 : 0);
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
			: -1
		: left.CompareTo(right);

	public static EsoVersion FromText(string text)
	{
		text = text?.Trim() ?? string.Empty;
		if (text.Length == 0)
		{
			return EsoVersion.Empty;
		}

		var pts = false;
		if (text.EndsWith("pts", StringComparison.Ordinal))
		{
			pts = true;
			text = text[..^3];
		}

		return int.TryParse(text, CultureInfo.InvariantCulture, out var version)
			? new EsoVersion(version, pts)
			: EsoVersion.Empty;
	}
	#endregion

	#region Public Methods
	public int CompareTo(object? obj) => this.CompareTo(obj as EsoVersion);

	public int CompareTo(EsoVersion? other) => other is null
		? 1
		: this.SortOrder.CompareTo(other.SortOrder);
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Text;
	#endregion
}
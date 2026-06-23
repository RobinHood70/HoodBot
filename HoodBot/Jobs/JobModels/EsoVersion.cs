namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

[StructLayout(LayoutKind.Auto)]
public readonly partial struct EsoVersion : IComparable<EsoVersion>, IEquatable<EsoVersion>
{
	#region Public Constructors
	public EsoVersion()
	{
		this.Version = 0;
		this.Pts = false;
	}

	public EsoVersion(int version, bool pts)
	{
		this.Version = version;
		this.Pts = pts && version != 0;
	}

	public EsoVersion(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		var textSplit = text.Trim().Split("pts", 2, StringSplitOptions.None);
		if (int.TryParse(textSplit[0], CultureInfo.InvariantCulture, out var version))
		{
			this.Version = version;
			this.Pts = textSplit.Length == 2;
		}
		else
		{
			// Empty string or string coincidentally ending in "pts".
			this.Version = Empty.Version;
			this.Pts |= Empty.Pts;
		}
	}
	#endregion

	#region Public Static Properties
	public static EsoVersion Empty => new();

	[GeneratedRegex(@"([0-9]+(pts)?)?\z", RegexOptions.ExplicitCapture, Globals.DefaultGeneratedRegexTimeout)]
	public static partial Regex VersionFinder { get; }
	#endregion

	#region Public Properties
	public int ActiveVersion => this.Pts ? this.Version - 1 : this.Version;

	/// <summary>Gets a value indicating whether this is a PTS version.</summary>
	/// <remarks>If Version is 0, Pts will always be false.</remarks>
	public bool Pts { get; }

	public int Version { get; }
	#endregion

	#region Private Properties

	// Two times the version, minus one if it's PTS
	private int SortOrder => this.Version == 0
		? 0
		: (this.Version << 1) - (this.Pts ? 1 : 0);
	#endregion

	#region Operator Overloads
	public static bool operator ==(EsoVersion left, EsoVersion right) => left.Equals(right);

	public static bool operator !=(EsoVersion left, EsoVersion right) => !(left == right);

	public static bool operator <(EsoVersion left, EsoVersion right) => left.CompareTo(right) < 0;

	public static bool operator >(EsoVersion left, EsoVersion right) => left.CompareTo(right) > 0;

	public static bool operator <=(EsoVersion left, EsoVersion right) => left.CompareTo(right) <= 0;

	public static bool operator >=(EsoVersion left, EsoVersion right) => left.CompareTo(right) >= 0;
	#endregion

	#region Public Methods
	public int CompareTo(EsoVersion other) => this.SortOrder.CompareTo(other.SortOrder);

	public bool Equals(EsoVersion other) => this.Version == other.Version && this.Pts == other.Pts;
	#endregion

	#region Public Override Methods
	public override bool Equals([NotNullWhen(true)] object? obj) => (obj is EsoVersion other) && this.Equals(other);

	public override int GetHashCode() => HashCode.Combine(this.Version, this.Pts);

	public override string ToString() => this.Version == 0
		? string.Empty
		: this.Version.ToStringInvariant() + (this.Pts ? "pts" : string.Empty);
	#endregion
}
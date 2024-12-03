namespace RobinHood70.HoodBot.Jobs;

using System.Globalization;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;

public class SFItem
{
	#region Constructors
	public SFItem(CsvRow row, string type)
	{
		// Slightly messy to have this one constructor try to adapt to all situations where it's used, but it's convenient. If things get worse, we can always split it out to a simple constructor for all values instead of the whole row.
		this.FormId = UespFunctions.FixFormId(row["FormID"]);
		this.EditorId = row["EditorID"];
		this.Type = type;
		this.Name = row["Name"];
		this.Description = row.TryGetValue("Description", out var description)
			? description
			: string.Empty;
		_ = row.TryGetValue("Value", out var value) || row.TryGetValue("Unknown1", out value);
		this.Value = int.Parse(value ?? "0", CultureInfo.CurrentCulture);
		if (double.TryParse(row["Weight"], CultureInfo.CurrentCulture, out var weight))
		{
			this.Weight = weight;
		}
	}
	#endregion

	#region Public Properties
	public string Description { get; }

	public string EditorId { get; }

	public string FormId { get; }

	public string Name { get; }

	public string Type { get; }

	public int Value { get; }

	public double Weight { get; }
	#endregion

	#region Public Override Properties
	public override string ToString() => this.Name;
	#endregion
}
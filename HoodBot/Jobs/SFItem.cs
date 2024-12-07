namespace RobinHood70.HoodBot.Jobs;

using System.Globalization;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;

public class SFItem
{
	#region Constructors
	public SFItem(CsvRow row, string type)
	{
		// These are common and reliable across all item types
		this.Description = row.TryGetValue("Description", out var description)
			? description
			: string.Empty;
		this.EditorId = row["EditorID"];
		this.OriginalFormId = row["FormID"];
		this.FormId = UespFunctions.FixFormId(this.OriginalFormId);
		this.Model = row["Model"];
		this.Name = row["Name"];
		this.Type = type;
		if (int.TryParse(row["Value"], CultureInfo.CurrentCulture, out var value))
		{
			this.Value = value;
		}

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

	public string Model { get; }

	public string Name { get; }

	public string OriginalFormId { get; }

	public string Type { get; }

	public int Value { get; }

	public double Weight { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion
}
namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Globalization;
using RobinHood70.CommonCode;

internal sealed class SFItem
{
	#region Constructors
	public SFItem(CsvRow row)
	{
		var formId = row["FormID"];
		if (formId.StartsWith("0x", StringComparison.Ordinal))
		{
			formId = formId[2..];
		}

		if (formId.Length != 8)
		{
			throw new InvalidOperationException();
		}

		if (!formId.StartsWith("00", StringComparison.Ordinal))
		{
			formId = "xx" + formId[2..];
		}

		this.FormId = formId;

		this.EditorId = row["EditorID"];
		this.Name = row["Name"];
		this.Description = row["Description"];
		if (double.TryParse(row["Weight"], CultureInfo.CurrentCulture, out var weight))
		{
			this.Weight = weight;
		}

		_ = row.TryGetValue("Value", out var value) || row.TryGetValue("Unknown1", out value);
		this.Value = int.Parse(value ?? "0", CultureInfo.CurrentCulture);
	}
	#endregion

	#region Public Properties
	public string Description { get; }

	public string EditorId { get; }

	public string FormId { get; }

	public string Name { get; }

	public int Value { get; }

	public double Weight { get; }
	#endregion

	#region Public Override Properties
	public override string ToString() => this.Name;
	#endregion
}
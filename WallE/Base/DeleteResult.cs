#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class DeleteResult
{
	#region Constructors
	internal DeleteResult(string title, string reason, long logId)
	{
		this.Title = title;
		this.Reason = reason;
		this.LogId = logId;
	}
	#endregion

	#region Public Properties
	public long LogId { get; }

	public string Reason { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}
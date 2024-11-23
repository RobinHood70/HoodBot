#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class DebugInfoLog(string caller, string logType, string message)
{
	#region Public Properties
	public string Caller { get; } = caller;

	public string LogType { get; } = logType;

	public string Message { get; } = message;
	#endregion

	#region Public Override Methods
	public override string ToString() => $"{this.LogType}: ({this.Caller}) {this.Message}";
	#endregion
}
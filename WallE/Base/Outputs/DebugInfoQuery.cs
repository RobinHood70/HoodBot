#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class DebugInfoQuery(string function, bool isMaster, double runTime, string sql)
	{
		#region Public Properties
		public string Function { get; } = function;

		public bool IsMaster { get; } = isMaster;

		public double RunTime { get; } = runTime;

		public string Sql { get; } = sql;
		#endregion

		#region Public Override Methods
		public override string ToString() => $"({this.Function}) {this.Sql}";
		#endregion
	}
}
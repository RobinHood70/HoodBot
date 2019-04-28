namespace RobinHood70.WikiClasses.Parser.Nodes
{
	public interface INodeBase
	{
		#region Methods
		void Accept(IVisitor visitor);
		#endregion
	}
}
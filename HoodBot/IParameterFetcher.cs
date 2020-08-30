namespace RobinHood70.HoodBot
{
	public interface IParameterFetcher
	{
		void GetParameter(ConstructorParameter parameter);

		void SetParameter(ConstructorParameter parameter);
	}
}
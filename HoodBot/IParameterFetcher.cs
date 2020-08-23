namespace RobinHood70.HoodBot
{
	internal interface IParameterFetcher
	{
		void GetParameter(ConstructorParameter parameter);

		void SetParameter(ConstructorParameter parameter);
	}
}
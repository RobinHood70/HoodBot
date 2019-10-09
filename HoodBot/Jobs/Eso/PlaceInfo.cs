namespace RobinHood70.HoodBot.Jobs.Eso
{
	public class PlaceInfo
	{
		public PlaceInfo(PlaceType placeType, string paramName, string category, int variesStart)
		{
			this.CategoryName = category;
			this.NpcParamName = paramName;
			this.Type = placeType;
			this.VariesStart = variesStart;
		}

		public string CategoryName { get; }

		public string NpcParamName { get; }

		public PlaceType Type { get; }

		public int VariesStart { get; }

		public void Deconstruct(out PlaceType placeType, out string paramName, out string category, out int variesStart)
		{
			placeType = this.Type;
			paramName = this.NpcParamName;
			category = this.CategoryName;
			variesStart = this.VariesStart;
		}
	}
}

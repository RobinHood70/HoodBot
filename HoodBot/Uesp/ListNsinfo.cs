namespace RobinHood70.HoodBot.Uesp
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.Eve.Modules;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class ListNsinfo(WikiAbstractionLayer wal, NsinfoInput input) : ListModule<NsinfoInput, NsinfoItem>(wal, input)
	{
		#region Public Override Properties
		public override int MinimumVersion => 0;

		public override string Name => "nsinfo";
		#endregion

		#region Protected Override Methods
		protected override string Prefix => "nsi";
		#endregion

		protected override void BuildRequestLocal(Request request, NsinfoInput input)
		{
		}

		protected override NsinfoItem? GetItem(JToken result) => result is null
			? null
			: new NsinfoItem(
				baseName: result.MustHaveString("base"),
				category: result.MustHaveString("category"),
				full: result.MustHaveString("full"),
				icon: result.MustHaveString("icon"),
				iconUrl: result.MustHaveString("iconurl"),
				id: result.MustHaveString("id"),
				isGamespace: result["isgamespace"].GetBCBool(),
				isModspace: result["ismodspace"].GetBCBool(),
				isPseudospace: result["ispseudospace"].GetBCBool(),
				mainPage: result.MustHaveString("mainpage"),
				modName: result.MustHaveString("modname"),
				modParent: result.MustHaveString("modparent"),
				name: result.MustHaveString("name"),
				nsId: (int)result.MustHave("nsid"),
				pageName: result.MustHaveString("pagename"),
				parent: result.MustHaveString("parent"),
				trail: result.MustHaveString("trail"));
	}
}

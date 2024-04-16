namespace RobinHood70.HoodBot
{
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Models;

	public class AppSettings : IJsonSettings<AppSettings>
	{
		#region Public Properties
		public IList<WikiInfo> DefaultWikis { get; } = [];

		public string FileName => Path.Combine(App.AppFolder, nameof(AppSettings) + ".json");
		#endregion

		#region Public Methods
		public void FromJson(JToken json)
		{
			if (json.NotNull()[nameof(this.DefaultWikis)] is JToken wikiNode && wikiNode.Type == JTokenType.Array)
			{
				foreach (var node in wikiNode)
				{
					var wiki = JsonSubSetting<WikiInfo>.FromJson(node);
					this.DefaultWikis.Add(wiki);
				}
			}
		}

		public JToken ToJson()
		{
			JArray wikis = [];
			foreach (var wiki in this.DefaultWikis)
			{
				wikis.Add(wiki.ToJson());
			}

			return new JObject
			{
				{ nameof(this.DefaultWikis), wikis }
			};
		}
		#endregion
	}
}
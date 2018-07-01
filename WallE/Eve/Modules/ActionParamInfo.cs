#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// MWVERSION: 1.28
	public class ActionParamInfo : ActionModule<ParameterInfoInput, IReadOnlyDictionary<string, ParameterInfoItem>>
	{
		#region Static Fields
		private static HashSet<string> formatModuleValues = new HashSet<string> { "json", "jsonfm", "php", "phpfm", "wddx", "wddxfm", "xml", "xmlfm", "yaml", "yamlfm", "rawfm", "txt", "txtfm", "dbg", "dbgfm", "dump", "dumpfm", "none" };
		#endregion

		#region Constructors
		public ActionParamInfo(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "paraminfo";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ParameterInfoInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			ThrowNull(input.Modules, nameof(input.Modules));
			if (this.SiteVersion >= 125)
			{
				request
					.Add("modules", input.Modules)
					.AddIfPositive("helpformat", input.HelpFormat);
				return;
			}

			var modules = new List<string>();
			var queryModules = new List<string>();
			var mainModule = false;
			var pagesetModule = false;
			var formatModules = new List<string>();

			foreach (var module in input.Modules)
			{
				if (module == "main")
				{
					mainModule = true;
				}
				else if (module == "pageset")
				{
					pagesetModule = true;
				}
				else if (formatModuleValues.Contains(module) && this.SiteVersion >= 119)
				{
					// This will mis-handle custom format modules on older wikis, but this possibility is insanely remote, so I don't feel the need to code for it.
					formatModules.Add(module);
				}
				else if (module.StartsWith("query+", StringComparison.Ordinal))
				{
					queryModules.Add(module.Split(new char[] { '+' }, 2)[1]);
				}
				else
				{
					modules.Add(module);
				}
			}

			request
				.Add("modules", modules)
				.Add("querymodules", queryModules)
				.Add("formatmodules", formatModules) // Version condition is handled in loop above, so no need to repeat it here
				.Add("mainmodule", mainModule)
				.Add("pagesetmodule", pagesetModule);
		}

		protected override IReadOnlyDictionary<string, ParameterInfoItem> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new Dictionary<string, ParameterInfoItem>();
			var moduleTypes = new List<string>() { "modules" };
			if (this.SiteVersion < 125)
			{
				moduleTypes.AddRange(new string[] { "querymodules", "formatmodules", "mainmodule", "pagesetmodule" });
			}

			foreach (var moduleType in moduleTypes)
			{
				var modules = result[moduleType];
				if (modules == null)
				{
					continue;
				}

				foreach (var module in modules)
				{
					var item = new ParameterInfoItem()
					{
						Name = (string)module["name"],
						ClassName = (string)module["classname"],
						Path = (string)module["path"],
						Group = (string)module["group"],
						Prefix = (string)module["prefix"],
						Source = (string)module["source"],
						SourceName = (string)module["sourcename"],
						LicenseTag = (string)module["licensetag"],
						LicenseLink = (string)module["licenselink"],
						Description = GetMessages(module["description"]),
						Flags =
						module.GetFlag("deprecated", ModuleFlags.Deprecated) |
						module.GetFlag("generator", ModuleFlags.Generator) |
						module.GetFlag("internal", ModuleFlags.Internal) |
						module.GetFlag("mustbeposted", ModuleFlags.MustBePosted) |
						module.GetFlag("readrights", ModuleFlags.ReadRights) |
						module.GetFlag("writerights", ModuleFlags.WriteRights),
						HelpUrls = module["helpurls"].AsReadOnlyList<string>(),
					};
					var examplesList = new List<ExamplesItem>();
					var examples = this.SiteVersion >= 125 ? module["examples"] : module["allexamples"];
					if (examples != null)
					{
						foreach (var example in examples)
						{
							var newExample = new ExamplesItem()
							{
								Query = (string)example["query"],
								Description = GetMessage(example["description"]),
							};
							examplesList.Add(newExample);
						}
					}

					item.Examples = examplesList;

					var parametersList = new Dictionary<string, ParametersItem>();
					var parameters = module["parameters"];
					if (parameters != null)
					{
						foreach (var parameterNode in parameters)
						{
							var parameter = new ParametersItem();
							var parameterName = (string)parameterNode["name"];
							parameter.Description = GetMessages(parameterNode["description"]);
							parameter.Flags =
								parameterNode.GetFlag("allowsduplicates", ParameterFlags.AllowsDuplicates) |
								parameterNode.GetFlag("deprecated", ParameterFlags.Deprecated) |
								parameterNode.GetFlag("enforcerange", ParameterFlags.EnforceRange) |
								parameterNode.GetFlag("multi", ParameterFlags.Multivalued) |
								parameterNode.GetFlag("required", ParameterFlags.Required);
							parameter.TokenType = (string)parameterNode["tokentype"];
							if (parameterNode["default"] is JValue defaultNode)
							{
								parameter.Default = defaultNode.Value;
							}

							parameter.Limit = (int?)parameterNode["limit"] ?? 0;
							parameter.HighLimit = (int?)parameterNode["highlimit"] ?? 0;
							parameter.LowLimit = (int?)parameterNode["lowlimit"] ?? 0;

							var typeValue = parameterNode["type"];
							if (typeValue != null)
							{
								if (typeValue.Type == JTokenType.Array)
								{
									parameter.TypeValues = typeValue.AsReadOnlyList<string>();
									parameter.Type = "valuelist";
									parameter.Submodules = parameterNode["submodules"].AsReadOnlyDictionary<string, string>();
									parameter.SubmoduleParameterPrefix = (string)parameterNode["submoduleparamprefix"];
								}
								else
								{
									parameter.TypeValues = new string[0];
									parameter.Type = (string)parameterNode["type"];
								}
							}

							parameter.Maximum = (int?)parameterNode["max"] ?? 0;
							parameter.HighMaximum = (int?)parameterNode["highmax"] ?? 0;
							parameter.Minimum = (int?)parameterNode["min"] ?? 0;

							var newInfoList = new List<InformationItem>();
							var infoArrayNode = parameterNode["info"];
							if (infoArrayNode != null)
							{
								foreach (var infoNode in infoArrayNode)
								{
									var info = new InformationItem()
									{
										Name = (string)infoNode["name"],
										Text = GetMessages(infoNode["text"]),
										Values = infoNode["values"].AsReadOnlyList<int>(),
									};
									newInfoList.Add(info);
								}
							}

							parameter.Information = newInfoList;

							parametersList.Add(parameterName, parameter);
						}
					}

					item.Parameters = parametersList;
					item.DynamicParameters = GetMessages(module["dynamicparameters"]);

					output.Add(item.Name, item);
				}
			}

			return output;
		}
		#endregion

		#region Private Methods
		private static RawMessageInfo GetMessages(JToken node)
		{
			var retval = new RawMessageInfo();
			if (node != null)
			{
				if (node.Type == JTokenType.String)
				{
					retval.Text = (string)node;
				}
				else
				{
					var messageList = new List<MessageItem>();
					foreach (var item in node)
					{
						var message = GetMessage(item);
						messageList.Add(message);
					}

					retval.RawMessages = messageList;
				}
			}

			return retval;
		}

		private static MessageItem GetMessage(JToken message)
		{
			var retval = new MessageItem()
			{
				Key = (string)message["key"],
				ForValue = (string)message["forvalue"],
			};
			var parameterList = new List<string>();
			var parameters = message["params"];
			if (parameters != null)
			{
				foreach (var parameter in parameters)
				{
					// Note that there can be objects in the parameter list, but the only instance so far is a totally redundant count of another node, so we're ignoring them for now, rather than building a more complex system here. There are no known instances of arrays, but if they do occur, this would choke, so we're filtering those out too.
					// TODO: Replace with a generic PHP data handler if one is developed.
					if (parameter.Type != JTokenType.Object && parameter.Type != JTokenType.Array)
					{
						parameterList.Add((string)parameter);
					}
				}
			}

			retval.Parameters = parameterList;

			return retval;
		}
		#endregion
	}
}

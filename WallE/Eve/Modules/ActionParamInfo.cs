#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// MWVERSION: 1.28
	internal class ActionParamInfo : ActionModule<ParameterInfoInput, IReadOnlyDictionary<string, ParameterInfoItem>>
	{
		#region Static Fields
		private static readonly HashSet<string> FormatModuleValues = new HashSet<string> { "json", "jsonfm", "php", "phpfm", "wddx", "wddxfm", "xml", "xmlfm", "yaml", "yamlfm", "rawfm", "txt", "txtfm", "dbg", "dbgfm", "dump", "dumpfm", "none" };
		private static readonly string[] ModuleTypes125 = { "querymodules", "formatmodules", "mainmodule", "pagesetmodule" };
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
					.Add("helpformat", input.HelpFormat);
				return;
			}

			var modules = new List<string>();
			var queryModules = new List<string>();
			var mainModule = false;
			var pagesetModule = false;
			var formatModules = new List<string>();

			foreach (var module in input.Modules)
			{
				if (module == "main" || module == "mainmodule")
				{
					mainModule = true;
				}
				else if (module == "pageset" || module == "pagesetmodule")
				{
					pagesetModule = true;
				}
				else if (FormatModuleValues.Contains(module) && this.SiteVersion >= 119)
				{
					// This will mis-handle custom format modules on older wikis, but this possibility is insanely remote, so I don't feel the need to code for it.
					formatModules.Add(module);
				}
				else if (module.StartsWith("query+", StringComparison.Ordinal))
				{
					queryModules.Add(module.Split(TextArrays.Plus, 2)[1]);
				}
				else
				{
					modules.Add(module);
				}
			}

			request
				.Add("modules", modules)
				.Add("querymodules", queryModules)
				.Add("formatmodules", formatModules) // Version condition is handled in loop, so no need to repeat it here
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
				moduleTypes.AddRange(ModuleTypes125);
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
					var description = GetMessages(module["description"]);
					var dynamicParameters = GetMessages(module["dynamicparameters"]);
					var examples = GetExamples(this.SiteVersion < 125 ? module["allexamples"] : module["examples"]);
					var parameters = GetParameters(module);
					var className = module.SafeString("classname");
					output.Add(className, new ParameterInfoItem(
						className: className,
						helpUrls: module["helpurls"].AsReadOnlyList<string>(),
						parameters: parameters,
						path: module.SafeString("path"),
						prefix: module.SafeString("prefix"),
						name: (string?)module["name"],
						description: description,
						dynamicParameters: dynamicParameters,
						examples: examples,
						flags:
							module.GetFlag("deprecated", ModuleFlags.Deprecated) |
							module.GetFlag("generator", ModuleFlags.Generator) |
							module.GetFlag("internal", ModuleFlags.Internal) |
							module.GetFlag("mustbeposted", ModuleFlags.MustBePosted) |
							module.GetFlag("readrights", ModuleFlags.ReadRights) |
							module.GetFlag("writerights", ModuleFlags.WriteRights),
						group: (string?)module["group"],
						licenseLink: (string?)module["licenselink"],
						licenseTag: (string?)module["licensetag"],
						source: (string?)module["source"],
						sourceName: (string?)module["sourcename"]
						));
				}
			}

			return output;
		}

		private static Dictionary<string, ParametersItem> GetParameters(JToken module)
		{
			var parametersList = new Dictionary<string, ParametersItem>();
			var parameters = module["parameters"];
			if (parameters != null)
			{
				foreach (var parameterNode in parameters)
				{
					var parameter = new ParametersItem();
					var parameterName = parameterNode.SafeString("name");
					parameter.Description = GetMessages(parameterNode["description"]);
					parameter.Flags =
						parameterNode.GetFlag("allowsduplicates", ParameterFlags.AllowsDuplicates) |
						parameterNode.GetFlag("deprecated", ParameterFlags.Deprecated) |
						parameterNode.GetFlag("enforcerange", ParameterFlags.EnforceRange) |
						parameterNode.GetFlag("multi", ParameterFlags.Multivalued) |
						parameterNode.GetFlag("required", ParameterFlags.Required);
					parameter.TokenType = (string?)parameterNode["tokentype"];
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
							parameter.Submodules = parameterNode["submodules"].AsReadOnlyDictionary<string>();
							parameter.SubmoduleParameterPrefix = (string?)parameterNode["submoduleparamprefix"];
						}
						else
						{
							parameter.TypeValues = Array.Empty<string>();
							parameter.Type = (string?)typeValue;
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
							newInfoList.Add(new InformationItem(
								name: infoNode.SafeString("name"),
								text: GetMessages(infoNode.NotNull("text")),
								values: infoNode["values"].AsReadOnlyList<int>()));
						}
					}

					parameter.Information = newInfoList;
					parametersList.Add(parameterName, parameter);
				}
			}

			return parametersList;
		}

		private static List<ExamplesItem> GetExamples(JToken? module)
		{
			var examplesList = new List<ExamplesItem>();
			if (module != null)
			{
				foreach (var example in module)
				{
					var newExample = new ExamplesItem(example.SafeString("query"), GetMessage(example.NotNull("description")));
					examplesList.Add(newExample);
				}
			}

			return examplesList;
		}
		#endregion

		#region Private Static Methods
		private static RawMessageInfo GetMessages(JToken? node)
		{
			var messageList = new List<MessageItem>();
			if (node != null)
			{
				if (node.Type == JTokenType.String)
				{
					return new RawMessageInfo((string?)node);
				}

				foreach (var item in node)
				{
					var message = GetMessage(item);
					messageList.Add(message);
				}
			}

			return new RawMessageInfo(messageList);
		}

		private static MessageItem GetMessage(JToken message)
		{
			// TODO: Perhaps add functionality to handle cases of parameter substitution (parameterList => num=1, comma-separated list, parameters) and perhaps even other advanced outputs, if any.
			var parameterList = new List<string>();
			var parameters = message["params"];
			if (parameters != null)
			{
				foreach (var parameter in parameters)
				{
					if (parameter != null)
					{
						switch (parameter.Type)
						{
							case JTokenType.String:
								parameterList.Add((string?)parameter ?? string.Empty);
								break;
							case JTokenType.Array:
								// No known instances of this happening, but it may be possible, so just add the raw JSON text for now.
								parameterList.Add(parameter.ToString());
								System.Diagnostics.Debug.WriteLine("Array: " + parameter.ToString());
								break;
							case JTokenType.Object:
								var mergeList = new List<string>();
								foreach (var kvp in parameter.Children<JProperty>())
								{
									mergeList.Add(kvp.Name + '=' + kvp.Value);
								}

								parameterList.Add(string.Join("|", mergeList));
								break;
							default:
								// Nothing else should be possible, so if it happens, just send it to the debug window for now.
								System.Diagnostics.Debug.WriteLine(parameter.Type.ToString() + ": " + parameter.ToString());
								break;
						}
					}
				}
			}

			return new MessageItem(
				key: message.SafeString("key"),
				parameters: parameterList,
				forValue: (string?)message["forvalue"]);
		}
		#endregion
	}
}
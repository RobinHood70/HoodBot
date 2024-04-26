namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// MWVERSION: 1.28
	internal sealed class ActionParamInfo(WikiAbstractionLayer wal) : ActionModule<ParameterInfoInput, IReadOnlyDictionary<string, ParameterInfoItem>>(wal)
	{
		#region Static Fields
		private static readonly HashSet<string> FormatModuleValues = new(StringComparer.Ordinal) { "json", "jsonfm", "php", "phpfm", "wddx", "wddxfm", "xml", "xmlfm", "yaml", "yamlfm", "rawfm", "txt", "txtfm", "dbg", "dbgfm", "dump", "dumpfm", "none" };
		private static readonly string[] ModuleTypes125 = ["querymodules", "formatmodules", "mainmodule", "pagesetmodule"];
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "paraminfo";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ParameterInfoInput input)
		{
			request.ThrowNull();
			input.ThrowNull();
			input.Modules.PropertyThrowNull(nameof(input), nameof(input.Modules));
			if (this.SiteVersion >= 125)
			{
				request
					.Add("modules", input.Modules)
					.Add("helpformat", input.HelpFormat);
				return;
			}

			List<string> modules = [];
			List<string> queryModules = [];
			var mainModule = false;
			var pagesetModule = false;
			List<string> formatModules = [];

			foreach (var module in input.Modules)
			{
				if (string.Equals(module, "main", StringComparison.Ordinal) || string.Equals(module, "mainmodule", StringComparison.Ordinal))
				{
					mainModule = true;
				}
				else if (string.Equals(module, "pageset", StringComparison.Ordinal) || string.Equals(module, "pagesetmodule", StringComparison.Ordinal))
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

		protected override IReadOnlyDictionary<string, ParameterInfoItem> DeserializeResult(JToken? result)
		{
			result.ThrowNull();
			Dictionary<string, ParameterInfoItem> output = new(StringComparer.Ordinal);
			List<string> moduleTypes = ["modules"];
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
					var parameters = GetParameters(module["parameters"]);
					var templatedParameters = GetParameters(module["templatedparameters"]);
					var className = module.MustHaveString("classname");
					output.Add(className, new ParameterInfoItem(
						className: className,
						path: module.MustHaveString("path"),
						prefix: module.MustHaveString("prefix"),
						helpUrls: module["helpurls"].GetList<string>(),
						parameters: parameters,
						name: (string?)module["name"],
						description: description,
						dynamicParameters: dynamicParameters,
						examples: examples,
						flags: module.GetFlags(
							("deprecated", ModuleFlags.Deprecated),
							("generator", ModuleFlags.Generator),
							("internal", ModuleFlags.Internal),
							("mustbeposted", ModuleFlags.MustBePosted),
							("readrights", ModuleFlags.ReadRights),
							("writerights", ModuleFlags.WriteRights)),
						group: (string?)module["group"],
						licenseLink: (string?)module["licenselink"],
						licenseTag: (string?)module["licensetag"],
						source: (string?)module["source"],
						sourceName: (string?)module["sourcename"],
						templatedParameters: templatedParameters));
				}
			}

			return output;
		}

		private static ReadOnlyKeyedCollection<string, ParametersItem> GetParameters(JToken? token)
		{
			List<ParametersItem> parametersList = [];
			if (token != null)
			{
				foreach (var parameterNode in token)
				{
					var dflt = parameterNode["default"] is JValue defaultNode ? defaultNode.Value : null;
					IReadOnlyList<string> typeValues = [];
					string? type = null;
					IReadOnlyDictionary<string, string> subModules = new Dictionary<string, string>(StringComparer.Ordinal);
					string? subModuleParamPrefix = null;
					if (parameterNode["type"] is JToken typeValue)
					{
						if (typeValue.Type == JTokenType.Array)
						{
							typeValues = typeValue.GetList<string>();
							type = "valuelist";
							subModules = parameterNode["submodules"].GetStringDictionary<string>();
							subModuleParamPrefix = (string?)parameterNode["submoduleparamprefix"];
						}
						else
						{
							type = (string?)typeValue;
						}
					}

					List<InformationItem> newInfoList = [];
					var infoArrayNode = parameterNode["info"];
					if (infoArrayNode != null)
					{
						foreach (var infoNode in infoArrayNode)
						{
							newInfoList.Add(new InformationItem(
								name: infoNode.MustHaveString("name"),
								text: GetMessages(infoNode.MustHave("text")),
								values: infoNode["values"].GetList<int>()));
						}
					}

					parametersList.Add(new ParametersItem(
						name: parameterNode.MustHaveString("name"),
						dflt: dflt,
						description: GetMessages(parameterNode["description"]),
						flags: parameterNode.GetFlags(
							("allowsduplicates", ParameterFlags.AllowsDuplicates),
							("deprecated", ParameterFlags.Deprecated),
							("enforcerange", ParameterFlags.EnforceRange),
							("multi", ParameterFlags.Multivalued),
							("required", ParameterFlags.Required)),
						highLimit: (int?)parameterNode["highlimit"] ?? 0,
						highMaximum: (int?)parameterNode["highmax"] ?? 0,
						information: newInfoList,
						limit: (int?)parameterNode["limit"] ?? 0,
						lowLimit: (int?)parameterNode["lowlimit"] ?? 0,
						maximum: (int?)parameterNode["max"] ?? 0,
						minimum: (int?)parameterNode["min"] ?? 0,
						subModuleParameterPrefix: subModuleParamPrefix,
						subModules: subModules,
						tokenType: (string?)parameterNode["tokentype"],
						type: type,
						typeValues: typeValues));
				}
			}

			return new ReadOnlyKeyedCollection<string, ParametersItem>(item => item.Name, parametersList, StringComparer.Ordinal);
		}

		private static List<ExamplesItem> GetExamples(JToken? module)
		{
			List<ExamplesItem> examplesList = [];
			if (module != null)
			{
				foreach (var example in module)
				{
					ExamplesItem newExample = new(example.MustHaveString("query"), GetMessage(example.MustHave("description")));
					examplesList.Add(newExample);
				}
			}

			return examplesList;
		}
		#endregion

		#region Private Static Methods
		private static RawMessageInfo GetMessages(JToken? node)
		{
			List<MessageItem> messageList = [];
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
			List<string> parameterList = [];
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
								List<string> mergeList = [];
								foreach (var kvp in parameter.Children<JProperty>())
								{
									mergeList.Add(kvp.Name + '=' + kvp.Value);
								}

								parameterList.Add(string.Join('|', mergeList));
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
				key: message.MustHaveString("key"),
				parameters: parameterList,
				forValue: (string?)message["forvalue"]);
		}
		#endregion
	}
}
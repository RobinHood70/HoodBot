namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;

	internal sealed class CastlesTranslator
	{
		#region Private Constants
		private const string SpaceSlash = " / ";
		#endregion

		#region Fields
		private readonly CultureInfo culture;
		private readonly Dictionary<string, string> languageEntries = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string> sentences = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string[]> terms = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string[]> variations = new(StringComparer.Ordinal);
		private string[] indefiniteArticles = [];
		private Regex? indefiniteArticleStarts;
		private string[] indefiniteExceptions = [];
		private Regex? indefiniteExceptionStarts;
		#endregion

		#region Constructors
		public CastlesTranslator(CultureInfo culture)
		{
			ArgumentNullException.ThrowIfNull(culture);
			this.culture = culture;
			this.LoadLanguageDatabase();
			this.LoadRules();
			this.LoadSentences();
			this.LoadTerms();
			this.LoadVariations();
		}
		#endregion

		#region Public Methods
		public string? GetLanguageEntry(string id, [CallerMemberName] string caller = "")
		{
			if (!this.languageEntries.TryGetValue(id, out var result))
			{
				Debug.WriteLine($"Language entry not found: {id} in {caller}()");
			}

			return result;
		}

		public string? GetSentence(string id, bool root, [CallerMemberName] string caller = "")
		{
			_ = this.sentences.TryGetValue(id, out var value);
			if (value is not null)
			{
				return this.Parse(value, root);
			}

			Debug.WriteLine($"Sentence not found: {id} in {caller}()");
			return null;
		}

		public string Parse(string input, bool root)
		{
			var braceSplit = input.Split(TextArrays.CurlyBrackets);

			// Check parentheses before we start modifying braceSplit
			for (var braceIndex = 1; braceIndex < braceSplit.Length; braceIndex += 2)
			{
				var parentheses = braceSplit.Length > 3 || braceSplit[braceIndex - 1].Length > 0 || (braceIndex + 1 < braceSplit.Length && braceSplit[braceIndex + 1].Length > 0);
				var parseInfo = new CastlesTxInfo(braceSplit[braceIndex]);
				if (parseInfo.Id.Length > 0)
				{
					var list = this.GetList(parseInfo);

					var sep = (root && braceSplit.Length == 3 && braceSplit[0].Length == 0 && braceSplit[2].Length == 0)
						? "<newline>"
						: SpaceSlash;
					var newTerm = string.Join(sep, list);
					parentheses &= list.Count > 1; // If single term, turn off parentheses
					if (parseInfo.Parent.Length > 0)
					{
						newTerm = $"{parseInfo.Parent}: {newTerm}";
						parentheses = true; // If parent exists, force parentheses on
					}

					if (parentheses && newTerm.Length > 0)
					{
						newTerm = '(' + newTerm + ')';
					}

					braceSplit[braceIndex] = newTerm;
				}
				else if (parseInfo.Target.Length > 0)
				{
					braceSplit[braceIndex] = $"(same as {parseInfo.Target}, above)";
				}
			}

			var retval = string.Join(string.Empty, braceSplit);
			if (root)
			{
				retval = retval.UpperFirst(culture, true);
			}

			return retval;
		}
		#endregion

		#region Private Static Methods
		private static List<string> GetPersonal(CastlesTxInfo parseInfo, [CallerMemberName] string caller = "")
		{
			var id = parseInfo.Id;
			var value = id switch
			{
				"name.given" => "First Name",
				"name.family" => "Family Name",
				_ => null,
			};

			if (value is null)
			{
				Debug.WriteLine($"Personal not found: {id} in {caller}()");
				value = id;
			}

			value = $"<{parseInfo.Target}'s {value}>";
			return [value];
		}
		#endregion

		#region Private Methods
		private List<string> GetList(CastlesTxInfo parseInfo)
		{
			var list =
				parseInfo.Variation ? this.GetVariations(parseInfo) :
				parseInfo.Term ? this.GetTerm(parseInfo, parseInfo.Id) :
				parseInfo.Personal ? GetPersonal(parseInfo) :
				null;
			if (list is null || list.Count == 0)
			{
				throw new InvalidOperationException("Unknown type: " + parseInfo.OriginalText);
			}

			for (var listIndex = list.Count - 1; listIndex >= 0; listIndex--)
			{
				if (list[listIndex].Length == 0)
				{
					list.RemoveAt(listIndex);
				}
			}

			return list;
		}

		private List<string>? GetTerm(CastlesTxInfo parseInfo, string id, [CallerMemberName] string caller = "")
		{
			_ = this.terms.TryGetValue(id, out var term);
			if (term is null)
			{
				Debug.WriteLine($"Term not found: {id} in {caller}()");
				return null;
			}

			bool[] include =
			[
				parseInfo.Male != false && parseInfo.Singular != false,
				parseInfo.Male != false && parseInfo.Singular != true,
				parseInfo.Male != true && parseInfo.Singular != false,
				parseInfo.Male != true && parseInfo.Singular != true,
			];

			var list = new List<string>();
			for (var i = 0; i < 4; i++)
			{
				var subTerm = term[i];
				if (include[i] && !list.Contains(subTerm, StringComparer.Ordinal))
				{
					list.Add(term[i]);
				}
			}

			return list;
		}

		private List<string>? GetVariations(CastlesTxInfo parseInfo)
		{
			if (!this.variations.TryGetValue(parseInfo.Id, out var variations))
			{
				return null;
			}

			var retval = new List<string>();
			if (parseInfo.Sentence)
			{
				foreach (var variation in variations)
				{
					if (this.GetSentence(variation, false) is string value)
					{
						retval.Add(value);
					}
				}
			}
			else if (parseInfo.Term)
			{
				foreach (var variation in variations)
				{
					if (this.GetTerm(parseInfo, variation) is List<string> value)
					{
						retval.AddRange(value);
					}
				}
			}
			else
			{
				Debug.WriteLine("Unknown variation type: " + parseInfo.OriginalText);
			}

			return retval;
		}

		private void LoadLanguageDatabase()
		{
			var text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\LanguageDatabase_Gameplay_{this.culture.IetfLanguageTag}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj.entries;
			foreach (var item in items)
			{
				this.languageEntries.TryAdd((string)item.key, (string)item.entry);
			}
		}

		private void LoadRules()
		{
			dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(@$"D:\Castles\MonoBehaviour\rules_{this.culture.IetfLanguageTag}.json")) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			var rules = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var item in items)
			{
				var key = (string)item._referenceId;
				var value = (string)item._data;
				rules.Add(key, value);
			}

			this.indefiniteArticleStarts = new Regex($@"\b({rules["article.indefinite.starts"]})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.indefiniteExceptionStarts = new Regex($@"\b({rules["article.indefinite.exception.starts"]})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.indefiniteArticles = rules["article.indefinite.format"].Split(TextArrays.Pipe);
			this.indefiniteExceptions = rules["article.indefinite.exception.format"].Split(TextArrays.Pipe);
		}

		private void LoadSentences()
		{
			var text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\sentences_{this.culture.IetfLanguageTag}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			foreach (var item in items)
			{
				this.sentences.Add((string)item._referenceId, (string)item._data);
			}
		}

		private void LoadTerms()
		{
			var text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\terms_{this.culture.IetfLanguageTag}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			foreach (var item in items)
			{
				var data = (string)item._data;
				var split = data.Split(TextArrays.Pipe);
				this.terms.Add((string)item._referenceId, split);
			}
		}

		private void LoadVariations()
		{
			var text = File.ReadAllText(@$"D:\Castles\MonoBehaviour\variations_{this.culture.IetfLanguageTag}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			foreach (var item in items)
			{
				var data = (string)item._data;
				var split = data.Split(TextArrays.Comma);
				var varList = new List<string>(split.Length);
				foreach (var id in split)
				{
					// Square breackets appear to indicate randomness weight.
					var stripped = id.Split('[', 2)[0];
					varList.Add(stripped);
				}

				this.variations.Add((string)item._referenceId, [.. varList]);
			}
		}
		#endregion
	}
}
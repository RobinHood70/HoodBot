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
		private readonly string[] indefiniteArticles = [];
		private readonly Regex indefiniteArticleStarts;
		private readonly string[] indefiniteExceptions = [];
		private readonly Regex indefiniteExceptionStarts;
		#endregion

		#region Constructors
		public CastlesTranslator(CultureInfo culture)
		{
			ArgumentNullException.ThrowIfNull(culture);
			this.culture = culture;
			this.LoadLanguageDatabase();

			var rules = this.LoadRules();
			this.indefiniteArticleStarts = new Regex($@"\A({rules["article.indefinite.starts"]})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.indefiniteExceptionStarts = new Regex($@"\A({rules["article.indefinite.exception.starts"]})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.indefiniteArticles = rules["article.indefinite.format"].Split(TextArrays.Pipe);
			this.indefiniteExceptions = rules["article.indefinite.exception.format"].Split(TextArrays.Pipe);

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
			if (!this.sentences.TryGetValue(id, out var value))
			{
				Debug.WriteLine($"Sentence not found: {id} in {caller}()");
				return null;
			}

			return this.Parse(value, root);
		}

		public string Parse(string input, bool root)
		{
			ArgumentNullException.ThrowIfNull(input);
			var braceSplit = input.Split(TextArrays.CurlyBrackets);

			// Check parentheses before we start modifying braceSplit
			for (var braceIndex = 1; braceIndex < braceSplit.Length; braceIndex += 2)
			{
				var parentheses = braceSplit.Length > 3 || braceSplit[braceIndex - 1].Length > 0 || (braceIndex + 1 < braceSplit.Length && braceSplit[braceIndex + 1].Length > 0);
				var txInfo = new CastlesTxInfo(braceSplit[braceIndex]);
				if (txInfo.Id.Length > 0)
				{
					var list = this.GetList(txInfo);

					var sep = (root && braceSplit.Length == 3 && braceSplit[0].Length == 0 && braceSplit[2].Length == 0)
						? "<newline>"
						: SpaceSlash;
					var newTerm = string.Join(sep, list);
					parentheses &= list.Count > 1; // If single term, turn off parentheses
					if (txInfo.Parent.Length > 0)
					{
						newTerm = $"{txInfo.Parent}: {newTerm}";
						parentheses = true; // If parent exists, force parentheses on
					}

					if (parentheses && newTerm.Length > 0)
					{
						newTerm = '(' + newTerm + ')';
					}

					braceSplit[braceIndex] = newTerm;
				}
				else if (txInfo.Target.Length > 0)
				{
					braceSplit[braceIndex] = $"(same as {txInfo.Target}, above)";
				}
			}

			var retval = string.Join(string.Empty, braceSplit);
			if (root)
			{
				retval = retval.UpperFirst(this.culture, true);
			}

			return retval;
		}
		#endregion

		#region Private Static Methods
		private static List<string> GetPersonal(CastlesTxInfo txInfo, [CallerMemberName] string caller = "")
		{
			var id = txInfo.Id;
			var value = id switch
			{
				"name.given" => "First Name",
				"name.family" => "Family Name",
				_ => null,
			};

			if (value is null)
			{
				Debug.WriteLine($"Personal not found: {id} in {caller}()");
				return [id];
			}

			value = $"<{txInfo.Target}'s {value}>";
			return [value];
		}
		#endregion

		#region Private Methods
		private List<string> GetList(CastlesTxInfo txInfo)
		{
			var list =
				txInfo.Variation ? this.GetVariations(txInfo) :
				txInfo.Term ? this.GetTerm(txInfo, txInfo.Id) :
				txInfo.Personal ? GetPersonal(txInfo) :
				null;
			if (list is null || list.Count == 0)
			{
				throw new InvalidOperationException("Unknown type: " + txInfo.OriginalText);
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

		private List<string>? GetTerm(CastlesTxInfo txInfo, string id, [CallerMemberName] string caller = "")
		{
			if (!this.terms.TryGetValue(id, out var term))
			{
				Debug.WriteLine($"Term not found: {id} in {caller}()");
				return null;
			}

			bool[] include =
			[
				txInfo.Male != false && txInfo.Singular != false,
				txInfo.Male != false && txInfo.Singular != true,
				txInfo.Male != true && txInfo.Singular != false,
				txInfo.Male != true && txInfo.Singular != true,
			];

			var list = new List<string>();
			for (var i = 0; i < 4; i++)
			{
				var word = term[i];
				if (include[i] && !list.Contains(word, StringComparer.Ordinal))
				{
					list.Add(word);
				}
			}

			if (txInfo.DefiniteArticle)
			{
				for (var i = 0; i < list.Count; i++)
				{
					// Naive implementation, since this is English-only for now.
					list[i] = (txInfo.Capitalize ? "The " : "the ") + list[i];
				}
			}
			else if (txInfo.IndefiniteArticle)
			{
				for (var i = 0; i < list.Count; i++)
				{
					list[i] = this.AddIndefiniteArticle(txInfo, list[i]);
				}
			}

			return list;
		}

		private List<string>? GetVariations(CastlesTxInfo txInfo)
		{
			if (!this.variations.TryGetValue(txInfo.Id, out var variations))
			{
				return null;
			}

			var retval = new List<string>();
			if (txInfo.Sentence)
			{
				foreach (var variation in variations)
				{
					if (this.GetSentence(variation, false) is string value)
					{
						retval.Add(value);
					}
				}
			}
			else if (txInfo.Term)
			{
				foreach (var variation in variations)
				{
					if (this.GetTerm(txInfo, variation) is List<string> value)
					{
						retval.AddRange(value);
					}
				}
			}
			else
			{
				Debug.WriteLine("Unknown variation type: " + txInfo.OriginalText);
			}

			return retval;
		}

		private string AddIndefiniteArticle(CastlesTxInfo txInfo, string word)
		{
			var index = txInfo.Male != false ? 2 : 0;
			index += txInfo.Singular switch
			{
				true => 0,
				false => 1,
				null => throw new InvalidOperationException()
			};

			// Checks must be in this exact order - do not try to combine anything.
			var indef =
				this.indefiniteExceptionStarts.Match(word).Success ? this.indefiniteExceptions[index] :
				this.indefiniteArticleStarts.Match(word).Success ? this.indefiniteArticles[index] :
				this.indefiniteExceptions[index];
			if (txInfo.Capitalize)
			{
				indef = indef.UpperFirst(this.culture);
			}

			return indef.Replace("{0}", word, StringComparison.Ordinal);
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

		private Dictionary<string, string> LoadRules()
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

			return rules;
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
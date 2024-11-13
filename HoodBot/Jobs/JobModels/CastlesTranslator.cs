namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;

	internal sealed class CastlesTranslator
	{
		#region Private Constants
		private const string SpaceSlash = " / ";
		#endregion

		#region Private Static Fields

		// For now, these are static English. Getting them from the files was either non-obvious or more trouble than it was worth.
		private static readonly string[] Relationships = ["Parent", "Child", "Sibling", "Aunt/Uncle", "Nephew/Niece", "Cousin", "Grandparent", "Grandchild", "Spouse", "Friend", "Lover", "Enemy"];
		private static readonly string[] Requesters = ["Ruler", "Requester", "Co-Requester"];
		#endregion

		#region Fields
		private readonly CultureInfo culture;
		private readonly Dictionary<string, string> languageEntries = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string> sentences = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string[]> terms = new(StringComparer.Ordinal);
		private readonly Dictionary<string, string[]> variations = new(StringComparer.Ordinal);
		private readonly Dictionary<bool, ArticleInfo> articleInfos = [];
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

		#region Public Properties
		public IDictionary<string, string> ParserOverrides { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
		#endregion

		#region Private Properties
		private string LangTag => this.culture.Name;
		#endregion

		#region Public Static Methods
		public static string GetRelatonship(int index) => index < 0 || index >= Relationships.Length
				? throw new ArgumentOutOfRangeException(nameof(index))
				: Relationships[index];

		public static string GetRequester(int index) => index < 0 || index >= Requesters.Length
				? throw new ArgumentOutOfRangeException(nameof(index))
				: Requesters[index];
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

		public string? GetSentence(string id, [CallerMemberName] string caller = "")
		{
			if (!this.sentences.TryGetValue(id, out var value))
			{
				Debug.WriteLine($"Sentence not found: {id} in {caller}()");
				return null;
			}

			return value;
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
					if (txInfo.Parent.Length > 0)
					{
						newTerm = $"{txInfo.Parent}: {newTerm}";
						parentheses = true; // If parent exists, force parentheses on
					}
					else
					{
						parentheses &= list.Count > 1; // If single term, turn off parentheses
					}

					if (newTerm.Length > 0)
					{
						if (this.ParserOverrides.TryGetValue(txInfo.Id, out var termOverride))
						{
							newTerm = termOverride.Replace("{0}", newTerm, StringComparison.Ordinal);
						}
						else if (parentheses)
						{
							newTerm = '(' + newTerm + ')';
						}
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

			if (txInfo.ArticleType is bool articleType)
			{
				for (var i = 0; i < list.Count; i++)
				{
					list[i] = this.articleInfos[articleType].AddArticleToWord(
						list[i],
						txInfo.Male ?? true,
						txInfo.Singular ?? throw new InvalidOperationException(),
						txInfo.Capitalize,
						this.culture);
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
					if (this.GetSentence(variation) is string value)
					{
						value = this.Parse(value, false);
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

		private void LoadLanguageDatabase()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + $"LanguageDatabase_Gameplay_{this.LangTag}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj.entries;
			foreach (var item in items)
			{
				this.languageEntries.TryAdd((string)item.key, (string)item.entry);
			}
		}

		private void LoadRules()
		{
			dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(GameInfo.Castles.ModFolder + $"rules_{this.LangTag}.json")) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			var rules = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var item in items)
			{
				var key = (string)item._referenceId;
				var value = (string)item._data;
				rules.Add(key, value);
			}

			// Indefinite articles
			var articleInfo = new ArticleInfo(
				rules["article.indefinite.format"],
				rules["article.indefinite.starts"],
				rules["article.indefinite.exception.format"],
				rules["article.indefinite.exception.starts"]);
			this.articleInfos.Add(false, articleInfo);

			// Definite articles
			articleInfo = new ArticleInfo(
				rules["article.indefinite.format"],
				rules["article.indefinite.starts"],
				rules["article.indefinite.exception.format"],
				rules["article.indefinite.exception.starts"]);
			this.articleInfos.Add(true, articleInfo);
		}

		private void LoadSentences()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + $"sentences_{this.LangTag}.json");
			dynamic obj = JsonConvert.DeserializeObject(text) ?? throw new InvalidOperationException();
			var items = obj._rawData;
			foreach (var item in items)
			{
				this.sentences.Add((string)item._referenceId, (string)item._data);
			}
		}

		private void LoadTerms()
		{
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + $"terms_{this.LangTag}.json");
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
			var text = File.ReadAllText(GameInfo.Castles.ModFolder + $"variations_{this.LangTag}.json");
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

		#region Private Classes
		private sealed class ArticleInfo(string articles, string articleStarts, string exceptions, string exceptionStarts)
		{
			#region Public Properties
			public string[] Articles { get; } = articles.Split(TextArrays.Pipe);

			public Regex ArticleStarts { get; } = new Regex($@"\A({articleStarts})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

			public string[] Exceptions { get; } = exceptions.Split(TextArrays.Pipe);

			public Regex ExceptionStarts { get; } = new Regex($@"\A({exceptionStarts})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			#endregion

			#region Public Methods
			public string AddArticleToWord(string word, bool male, bool singular, bool capitalize, CultureInfo culture)
			{
				var declension = (male ? 0 : 2) + (singular ? 0 : 1);

				// Checks must be in this exact order - do not optimize.
				var retval =
					this.ExceptionStarts.Match(word).Success ? this.Exceptions[declension] :
					this.ArticleStarts.Match(word).Success ? this.Articles[declension] :
					this.Exceptions[declension];
				if (capitalize)
				{
					retval = retval.UpperFirst(culture);
				}

				return retval.Replace("{0}", word, StringComparison.Ordinal);
			}
			#endregion
		}
		#endregion
	}
}